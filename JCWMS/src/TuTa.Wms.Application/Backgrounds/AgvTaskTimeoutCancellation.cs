using System;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks;
using TuTa.Wms.AgvTasks.Aggregaes;
using TuTa.Wms.AgvTasks.Dto;

namespace TuTa.Wms.Backgrounds;

/// <summary>十二小时未结束任务的筛选与取消确认规则。</summary>
internal static class AgvTaskTimeoutCancellation
{
    /// <summary>生成可翻译为数据库查询的超时筛选条件，now 使用与任务创建时间相同的系统时钟。</summary>
    internal static Expression<Func<AgvTask, bool>> ExpiredTasks(DateTime now)
    {
        var cutoff = now.AddHours(-12);
        return task => !task.IsDeleted && task.CreationTime <= cutoff
            && task.AgvTaskStatus != AgvTaskStatus.Complete
            && task.AgvTaskStatus != AgvTaskStatus.Cancel
            && task.AgvTaskStatus != AgvTaskStatus.ExceptionComplete;
    }

    /// <summary>取消超时任务；RCS 确认后才允许本地取消，提交成功后停止监听。</summary>
    /// <param name="task">本轮扫描获得的任务快照。</param>
    /// <param name="now">当前业务时钟时间。</param>
    /// <param name="cancelRcs">接收取消请求编号及原任务编号，向 RCS 发起取消。</param>
    /// <param name="cancelLocal">按任务 ID 重新核对状态并在独立工作单元提交取消；已结束时返回 false。</param>
    /// <param name="stopMonitor">本地取消成功后停止该任务机台监听。</param>
    /// <returns>本次成功提交本地取消时为 true，否则为 false。</returns>
    internal static async Task<bool> CancelAsync(AgvTask task, DateTime now,
        Func<string, string, Task<ResultAgvTaskDto>> cancelRcs, Func<int, Task<bool>> cancelLocal, Action<string> stopMonitor)
    {
        if (task == null || !ExpiredTasks(now).Compile()(task))
            return false;

        ArgumentException.ThrowIfNullOrWhiteSpace(task.ReqCode);
        // 每个任务的超时取消为独立逻辑请求；跨扫描重试沿用编号，避免重复取消。
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"timeout-cancel:{task.ReqCode}"));
        var reqCode = Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
        var response = await cancelRcs(reqCode, task.ReqCode);
        if (response?.Code != "0")
            throw new InvalidOperationException($"RCS 超时任务取消未确认，任务：{task.ReqCode}，reqCode：{reqCode}，返回码：{response?.Code ?? "空"}，消息：{response?.Message ?? "空响应"}");

        if (!await cancelLocal(task.Id))
            return false;

        stopMonitor(task.ReqCode);
        return true;
    }
}
