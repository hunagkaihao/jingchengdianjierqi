using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks;
using TuTa.Wms.AgvTasks.Aggregaes;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace TuTa.Wms.Backgrounds;

/// <summary>
/// 启动后扫描一次，此后每小时串行扫描；对创建满十二小时且尚未结束的任务发起 RCS 取消。
/// 每次查询及本地取消使用独立作用域和工作单元，避免后台长期持有 DbContext。
/// </summary>
public class AgvTaskTimeoutBackgroundService : BackgroundService
{
    /// <summary>超时任务扫描周期，单位为小时。</summary>
    internal static readonly TimeSpan ScanInterval = TimeSpan.FromHours(1);

    /// <summary>为扫描、远程调用及逐条取消创建独立依赖作用域。</summary>
    private readonly IServiceScopeFactory _scopeFactory;
    /// <summary>记录超时取消结果，单条失败不会中断整轮扫描。</summary>
    private readonly ILogger<AgvTaskTimeoutBackgroundService> _logger;
    /// <summary>本地取消提交后停止对应任务的机台监听。</summary>
    private readonly AgvTaskMonitorManager _monitorManager;

    /// <summary>初始化超时取消后台服务，不在构造器中解析数据库作用域依赖。</summary>
    public AgvTaskTimeoutBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<AgvTaskTimeoutBackgroundService> logger, AgvTaskMonitorManager monitorManager)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _monitorManager = monitorManager;
    }

    /// <summary>首次立即扫描，然后按小时运行；服务停止时结束定时器和后续处理。</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var timer = new PeriodicTimer(ScanInterval);
        try
        {
            do
            {
                try
                {
                    await ScanAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "超时 AGV 任务扫描失败，将在下一小时重试");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 正常停止服务，不再启动新的任务取消请求。
        }
    }

    /// <summary>读取超时任务快照并逐条取消，RCS 调用期间不占用数据库事务。</summary>
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        List<AgvTask> tasks;
        DateTime now;
        using (var scope = _scopeFactory.CreateScope())
        {
            var rcs = scope.ServiceProvider.GetRequiredService<RcsApiManager>();
            if (!string.Equals(rcs.AGVEnable, "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("AGV 未启用，跳过超时任务自动取消");
                return;
            }

            now = scope.ServiceProvider.GetRequiredService<IClock>().Now;
            using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
                .Begin(requiresNew: true, isTransactional: false);
            var repository = scope.ServiceProvider.GetRequiredService<IAgvTaskRepository>();
            tasks = await repository.GetListAsync(AgvTaskTimeoutCancellation.ExpiredTasks(now),
                includeDetails: false, cancellationToken: cancellationToken);
            await uow.CompleteAsync(cancellationToken);
        }

        _logger.LogInformation("十二小时未结束 AGV 任务扫描完成，待处理数量: {Count}", tasks.Count);
        foreach (var snapshot in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                // 发起远程取消前重新读取，跳过扫描后已被完成、取消或删除的任务。
                var current = await ReadCurrentTaskAsync(snapshot.Id, cancellationToken);
                var clock = scope.ServiceProvider.GetRequiredService<IClock>();
                var rcs = scope.ServiceProvider.GetRequiredService<RcsApiManager>();
                var cancelled = await AgvTaskTimeoutCancellation.CancelAsync(current, clock.Now,
                    (reqCode, taskCode) => rcs.CancelTaskAsync(reqCode, taskCode),
                    id => CancelLocalAsync(id, cancellationToken),
                    taskCode => { _monitorManager.TryCancel(taskCode); });
                if (cancelled)
                    _logger.LogInformation("AGV 任务超时自动取消成功，任务编号: {TaskCode}，创建时间: {CreationTime}",
                        snapshot.ReqCode, snapshot.CreationTime);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AGV 任务超时自动取消失败，任务编号: {TaskCode}，下一小时重新核对处理", snapshot.ReqCode);
            }
        }
    }

    /// <summary>用独立工作单元读取最新状态，避免使用上一轮扫描的跟踪实体。</summary>
    private async Task<AgvTask> ReadCurrentTaskAsync(int taskId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
            .Begin(requiresNew: true, isTransactional: false);
        var task = await scope.ServiceProvider.GetRequiredService<IAgvTaskRepository>()
            .FindByIdAsync(taskId, isTrack: false, cancellationToken: cancellationToken);
        await uow.CompleteAsync(cancellationToken);
        return task;
    }

    /// <summary>RCS 确认后再次核对状态，复用原取消逻辑释放库位，提交后再由调用方停止监听。</summary>
    private async Task<bool> CancelLocalAsync(int taskId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
            .Begin(requiresNew: true, isTransactional: true);
        var task = await scope.ServiceProvider.GetRequiredService<IAgvTaskRepository>()
            .FindByIdAsync(taskId, cancellationToken: cancellationToken);
        var now = scope.ServiceProvider.GetRequiredService<IClock>().Now;
        if (task == null || !AgvTaskTimeoutCancellation.ExpiredTasks(now).Compile()(task))
            return false;

        // 已单独得到 RCS 成功响应，本地取消不再发起第二次远程取消。
        await scope.ServiceProvider.GetRequiredService<AgvTaskManager>().SetAsCancelAsync(task.ReqCode, isActiveCancel: false);
        await uow.CompleteAsync(cancellationToken);
        return true;
    }
}
