using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks;
using TuTa.Wms.AgvTasks.Aggregaes;
using TuTa.Wms.Cells;
using TuTa.Wms.Cells.Aggregates;
using TuTa.Wms.Machines.Aggregates;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Wms.ConfigTool;
using Wms.LogTool;
using Wms.ModbusTool;
using Wms.RedisTool;

namespace TuTa.Wms.Backgrounds;

public class MachineStatusBackgroundService : IHostedService, IDisposable
{
    private readonly ModbusHelper _modbusHelper;
    private readonly UnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<MachineStatusBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AgvTaskManager _agvTaskManager;
    private readonly IRepository<Machine, int> _machineRepository;
    private readonly IRepository<MachinePoint, int> _machinePointRepository;
    private readonly IRepository<Cell, Guid> _cellRepository;
    private readonly IRepository<AgvTask, int> _agvTaskRepository;
    private readonly IAgvTaskService _agvTaskService;
    private readonly IRedisClient _redisClient;
    private readonly IOptions<ConfigOptions> _options;
    private readonly List<MachineConfig> _machineConfigs = new List<MachineConfig>();
    private readonly ConcurrentDictionary<string, bool> _lastFullMaterialStatus = new ConcurrentDictionary<string, bool>();
    private readonly object _configLock = new object();

    /// <summary>
    /// 配置热更新标记（1 = 需要重新加载机台配置）
    /// </summary>
    private static int _configReloadNeeded;

    /// <summary>
    /// 请求后台监控在下一轮循环重新加载数据库中的机台配置（用于机台启用/停用后 10 秒内生效）
    /// </summary>
    public static void RequestConfigReload()
    {
        Interlocked.Exchange(ref _configReloadNeeded, 1);
    }

    /// <summary>
    /// 机台状态在 Redis 中的 Hash 键名
    /// </summary>
    public const string MachineStatusRedisKey = "Wms:MachineStatus";

    public MachineStatusBackgroundService(
        ModbusHelper modbusHelper,
        UnitOfWorkManager unitOfWorkManager,
        ILogger<MachineStatusBackgroundService> logger,
        IConfiguration configuration,
        AgvTaskManager agvTaskManager,
        IRepository<Machine, int> machineRepository,
        IRepository<MachinePoint, int> machinePointRepository,
        IRepository<Cell, Guid> cellRepository,
        IRepository<AgvTask, int> agvTaskRepository,
        IAgvTaskService agvTaskService,
        IRedisClient redisClient,
        IOptions<ConfigOptions> options)
    {
        _modbusHelper = modbusHelper;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
        _configuration = configuration;
        _agvTaskManager = agvTaskManager;
        _machineRepository = machineRepository;
        _machinePointRepository = machinePointRepository;
        _cellRepository = cellRepository;
        _agvTaskRepository = agvTaskRepository;
        _agvTaskService = agvTaskService;
        _redisClient = redisClient;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("机台状态监控服务启动");

        // 连接 Redis（WMS 默认库），用于写入机台状态
        try
        {
            _redisClient.Build(_options.Value.RedisConnStr, _options.Value.DefaultRedisNo);
            // 启动时清空旧的机台状态缓存，避免残留过期数据
            var oldFields = _redisClient.GetHashFields(MachineStatusRedisKey);
            if (oldFields.Length > 0)
            {
                _redisClient.RemoveHashFields(MachineStatusRedisKey, oldFields);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "机台状态监控服务连接 Redis 失败: {Message}", ex.Message);
        }

        Task.Run(async () =>
        {
            try
            {
                await LoadMachineConfigsFromDatabaseAsync();

                _logger.LogInformation("监控设备数量: {Count}, 设备列表: {Machines}",
                    _machineConfigs.Count,
                    string.Join(", ", _machineConfigs.Select(c => $"{c.MachineName}({c.IpAddress}:{c.Port})")));

                var lastConfigLoadTime = DateTime.Now;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // 机台启用/停用后热更新监控配置（10 秒内生效）
                    if (Interlocked.CompareExchange(ref _configReloadNeeded, 0, 1) == 1)
                    {
                        await LoadMachineConfigsFromDatabaseAsync();
                        RemoveDisabledMachineStatusFromRedis();
                        lastConfigLoadTime = DateTime.Now;
                    }
                    // 定期兜底刷新配置，自动感知数据库中直接新增/变更的机台
                    else if ((DateTime.Now - lastConfigLoadTime).TotalMinutes >= 5)
                    {
                        await LoadMachineConfigsFromDatabaseAsync();
                        RemoveDisabledMachineStatusFromRedis();
                        lastConfigLoadTime = DateTime.Now;
                    }

                    await ReadMachineStatuses();
                    await Task.Delay(10000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("机台状态监控服务已取消");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "机台状态监控服务异常终止");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    private async Task LoadMachineConfigsFromDatabaseAsync()
    {
        try
        {
            using (var uow = _unitOfWorkManager.Begin())
                {
                    var machines = await _machineRepository.GetListAsync(m => m.IsEnabled);
                    if (machines == null || machines.Count == 0)
                    {
                        _logger.LogWarning("没有找到启用的机台");
                        lock (_configLock)
                        {
                            _machineConfigs.Clear();
                        }
                        return;
                    }

                    var newConfigs = new List<MachineConfig>();

                    foreach (var machine in machines)
                    {
                        if (string.IsNullOrEmpty(machine.IpAddress))
                        {
                            _logger.LogWarning("机台 {MachineName} 未配置IP地址，跳过监控", machine.Name);
                            continue;
                        }

                        var config = new MachineConfig
                        {
                            MachineName = machine.Name,
                            IpAddress = machine.IpAddress,
                            Port = machine.Port,
                            SlaveId = machine.SlaveId,
                            MachineCellCode = machine.Name
                        };

                        var machinePoints = await _machinePointRepository.GetListAsync(p => p.MachineId == machine.Id);
                        if (machinePoints == null || machinePoints.Count == 0)
                        {
                            _logger.LogWarning("机台 {MachineName} 未配置门站点，跳过监控", machine.Name);
                            continue;
                        }

                        var doorGroups = machinePoints.GroupBy(p => p.DoorNumber);
                        foreach (var group in doorGroups)
                        {
                            int doorNumber = group.Key;
                            var point = group.First();

                            if (doorNumber == 1)
                            {
                                config.Gate1FullMaterialAddress = point.FullMaterialAddress;
                                config.Gate1CellCode = point.CellCode;
                                config.Gate1TaskType = point.TaskType;
                            }
                            else if (doorNumber == 2)
                            {
                                config.Gate2FullMaterialAddress = point.FullMaterialAddress;
                                config.Gate2CellCode = point.CellCode;
                                config.Gate2TaskType = point.TaskType;
                            }
                        }

                        newConfigs.Add(config);
                    }

                    // 整体替换监控配置，避免与监控循环并发读写冲突
                    lock (_configLock)
                    {
                        _machineConfigs.Clear();
                        _machineConfigs.AddRange(newConfigs);
                    }

                    _logger.LogInformation("从数据库加载了 {Count} 个可用设备配置", newConfigs.Count);

                    await uow.CompleteAsync();
                }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载设备配置失败");
        }
    }

    private async Task ReadMachineStatuses()
    {
        // 取快照，避免与热更新替换配置并发冲突
        List<MachineConfig> configs;
        lock (_configLock)
        {
            if (_machineConfigs.Count == 0)
                return;
            configs = _machineConfigs.ToList();
        }

        var tasks = configs.Select(config => ReadMachineStatus(config)).ToList();
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 清理已停用机台的 Redis 状态与内存缓存（热更新配置后调用）
    /// </summary>
    private void RemoveDisabledMachineStatusFromRedis()
    {
        try
        {
            var enabledNames = new HashSet<string>();
            lock (_configLock)
            {
                foreach (var config in _machineConfigs)
                {
                    enabledNames.Add(config.MachineName);
                }
            }

            // 清理 Redis 中已停用机台的状态，避免前端展示停用机台的过期数据
            var fields = _redisClient.GetHashFields(MachineStatusRedisKey);
            var staleFields = fields.Where(f =>
            {
                var idx = f.LastIndexOf('_');
                var machineName = idx > 0 ? f.Substring(0, idx) : f;
                return !enabledNames.Contains(machineName);
            }).ToArray();

            if (staleFields.Length > 0)
            {
                _redisClient.RemoveHashFields(MachineStatusRedisKey, staleFields);
                _logger.LogInformation("已清理 {Count} 个停用机台的 Redis 状态", staleFields.Length);
            }

            // 清理内存缓存，确保重新启用后按新配置重新加载
            foreach (var machineName in _machinePointsCache.Keys)
            {
                if (!enabledNames.Contains(machineName))
                {
                    _machinePointsCache.TryRemove(machineName, out _);
                }
            }

            foreach (var key in _lastFullMaterialStatus.Keys)
            {
                var idx = key.LastIndexOf('_');
                var machineName = idx > 0 ? key.Substring(0, idx) : key;
                if (!enabledNames.Contains(machineName))
                {
                    _lastFullMaterialStatus.TryRemove(key, out _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理停用机台状态失败");
        }
    }

    private readonly ConcurrentDictionary<string, List<MachinePoint>> _machinePointsCache = new ConcurrentDictionary<string, List<MachinePoint>>();

    private async Task ReadMachineStatus(MachineConfig config)
    {
        var machineName = config.MachineName;
        var ipAddress = config.IpAddress;
        var port = config.Port;
        var slaveId = config.SlaveId;

        try
        {
            if (!_machinePointsCache.TryGetValue(machineName, out var machinePoints))
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    var machine = await _machineRepository.GetListAsync(m => m.Name == machineName);
                    if (machine == null || machine.Count == 0)
                    {
                        _logger.LogError("找不到机台 {MachineName} 的数据库记录", machineName);
                        return;
                    }

                    var machineId = machine[0].Id;
                    machinePoints = await _machinePointRepository.GetListAsync(p => p.MachineId == machineId);
                    _machinePointsCache[machineName] = machinePoints;

                    await uow.CompleteAsync();
                }
            }

            if (machinePoints == null || machinePoints.Count == 0)
                return;

            var validPoints = machinePoints.Where(p =>
                !string.IsNullOrEmpty(p.CellCode) &&
                !string.IsNullOrEmpty(p.TaskType) &&
                p.FullMaterialAddress > 0
            ).ToList();

            if (validPoints.Count == 0)
                return;

            // 查询门库位关联的进行中任务（只读展示用，故障隔离：查询失败仅记日志，不影响信号监控主流程）
            Dictionary<string, AgvTask> doorTasks = null;
            try
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    doorTasks = await GetDoorActiveTaskMapAsync(validPoints.Select(p => p.CellCode).ToList());
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查询机台 {MachineName} 门任务状态失败，本轮任务状态显示为未知", machineName);
            }

            foreach (var point in validPoints)
            {
                await ProcessDoorStatus(config, point, doorTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取机台 {MachineName}({Ip}:{Port}) 状态失败", machineName, ipAddress, port);
        }
    }

    private async Task ProcessDoorStatus(MachineConfig config, MachinePoint point, Dictionary<string, AgvTask> doorTasks)
    {
        var machineName = config.MachineName;
        var ipAddress = !string.IsNullOrEmpty(point.IpAddress) ? point.IpAddress : config.IpAddress;
        var port = point.Port > 0 ? point.Port : config.Port;
        var slaveId = point.SlaveId > 0 ? point.SlaveId : config.SlaveId;
        var doorName = $"{point.DoorNumber}号门";
        var key = $"{machineName}_{point.DoorNumber}";

        try
        {
            // 读取满料信号（可区分断线与正常false）
            var (readSuccess, isFullMaterial) = await _modbusHelper.ReadCoilWithStatusAsync(ipAddress, port, slaveId, point.FullMaterialAddress);

            // 写入机台状态到 Redis（无论是否断线都写，前端据此区分断线/未触发上料）
            WriteMachineStatusToRedis(machineName, point.DoorNumber, point, readSuccess, readSuccess && isFullMaterial, doorTasks);

            if (!readSuccess)
            {
                _logger.LogWarning("机台 {MachineName} {DoorName}({Ip}:{Port}) 满料信号读取失败(断线), 地址: {Address}",
                    machineName, doorName, ipAddress, port, point.FullMaterialAddress);
                _lastFullMaterialStatus[key] = false;
                return;
            }

            bool lastStatus = _lastFullMaterialStatus.TryGetValue(key, out var value) ? value : false;

            // 状态变化时记录日志，便于排查
            if (isFullMaterial != lastStatus)
            {
                _logger.LogInformation("机台 {MachineName} {DoorName} 满料状态变化: {Last} -> {Current}",
                    machineName, doorName, lastStatus, isFullMaterial);
            }

            // 只有当状态从 false 变为 true 时才触发任务
            if (isFullMaterial && !lastStatus)
            {
                _logger.LogInformation("机台 {MachineName} {DoorName} 触发任务: {TaskType}, 库位: {CellCode}",
                    machineName, doorName, point.TaskType, point.CellCode);

                using (var uow = _unitOfWorkManager.Begin())
                {
                    var cell = await _cellRepository.FindAsync(s => s.CellCode == point.CellCode);
                    if (cell != null && cell.RunStatus == CellRunStatus.Selected)
                    {
                        _logger.LogWarning("库位 {CellCode} 被锁定，跳过任务发送", point.CellCode);
                        _lastFullMaterialStatus[key] = isFullMaterial;
                        return;
                    }

                    try
                    {
                        switch (point.TaskType)
                        {
                            case "EmptyBoxToMachine":
                                await _agvTaskService.CreateEmptyBoxToMachineTaskAsync(point.CellCode);
                                break;
                            case "FinishedFromMachine":
                                await _agvTaskService.CreateFinishedFromMachineTaskAsync(point.CellCode, machineName);
                                break;
                            case "EmptyBoxFromMachine":
                                await _agvTaskService.CreateEmptyBoxFromMachineTaskAsync(point.CellCode);
                                break;
                            case "SemiFinishedFromMachine":
                                await _agvTaskService.CreateSemiFinishedFromMachineTaskAsync(point.CellCode, machineName);
                                break;
                            default:
                                _logger.LogWarning("机台 {MachineName} {DoorName} 未知任务类型: {TaskType}", machineName, doorName, point.TaskType);
                                return;
                        }

                        _logger.LogInformation("机台 {MachineName} {DoorName} {TaskType} 任务创建成功, 库位: {CellCode}",
                            machineName, doorName, point.TaskType, point.CellCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "机台 {MachineName} {DoorName} 任务创建失败, 任务类型: {TaskType}, 库位: {CellCode}",
                            machineName, doorName, point.TaskType, point.CellCode);
                        throw;
                    }

                    await uow.CompleteAsync();
                }
            }

            _lastFullMaterialStatus[key] = isFullMaterial;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取机台 {MachineName} {DoorName}({Ip}:{Port}) 满料信号失败, 地址: {Address}",
                machineName, doorName, ipAddress, port, point.FullMaterialAddress);
        }
    }

    /// <summary>
    /// 将机台门状态写入 Redis Hash（key: Wms:MachineStatus, field: 机台名_门号）
    /// </summary>
    /// <param name="machineName">机台名称</param>
    /// <param name="doorNumber">门号</param>
    /// <param name="point">机台站点</param>
    /// <param name="online">是否在线（Modbus读取成功）</param>
    /// <param name="fullMaterial">满料信号值（断线时为false）</param>
    /// <param name="doorTasks">门库位进行中任务映射（null 表示查询失败，任务状态显示"未知"）</param>
    private void WriteMachineStatusToRedis(string machineName, int doorNumber, MachinePoint point, bool online, bool fullMaterial, Dictionary<string, AgvTask> doorTasks)
    {
        try
        {
            // 解析当前门库位的进行中任务（doorTasks 为 null 表示任务查询失败；空映射表示无进行中任务）
            var hasTaskInfo = doorTasks != null;
            AgvTask doorTask = null;
            if (hasTaskInfo)
            {
                doorTasks.TryGetValue(point.CellCode, out doorTask);
            }

            var status = new
            {
                machineName,
                doorNumber,
                doorName = $"{doorNumber}号门",
                cellCode = point.CellCode,
                taskType = point.TaskType,
                fullMaterialAddress = point.FullMaterialAddress,
                // 在线且满料信号为 true 才是"已触发上料"
                fullMaterial,
                online,
                lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                // 任务状态展示（只读查看，不影响业务）
                taskId = doorTask?.Id,
                taskStatus = doorTask != null ? (int)doorTask.AgvTaskStatus : (int?)null,
                taskStatusText = !hasTaskInfo ? "未知" : (doorTask != null ? GetTaskStatusText(doorTask.AgvTaskStatus) : "无任务")
            };
            _redisClient.SetHashValue(MachineStatusRedisKey, $"{machineName}_{doorNumber}", JsonConvert.SerializeObject(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入机台状态到 Redis 失败: {MachineName}_{DoorNumber}", machineName, doorNumber);
        }
    }

    /// <summary>
    /// 查询门库位关联的进行中任务（纯只读查询，不影响业务），key: 库位编码, value: 最新一条进行中任务
    /// </summary>
    private async Task<Dictionary<string, AgvTask>> GetDoorActiveTaskMapAsync(List<string> cellCodes)
    {
        var result = new Dictionary<string, AgvTask>();

        var tasks = await _agvTaskRepository.GetListAsync(t =>
            cellCodes.Contains(t.StartPositionCode) || cellCodes.Contains(t.EndPositionCode));

        // 过滤已结束的任务（完成/取消/异常完成），同一门多条进行中任务时取最新一条
        foreach (var task in tasks.Where(t => !IsTerminalTaskStatus(t.AgvTaskStatus)).OrderByDescending(t => t.Id))
        {
            // 上机台任务 EndPositionCode 为门库位，下机台任务 StartPositionCode 为门库位，优先按终点匹配
            var cell = cellCodes.Contains(task.EndPositionCode) ? task.EndPositionCode : task.StartPositionCode;
            if (!string.IsNullOrEmpty(cell) && !result.ContainsKey(cell))
            {
                result[cell] = task;
            }
        }

        return result;
    }

    private static bool IsTerminalTaskStatus(AgvTaskStatus status)
    {
        return status == AgvTaskStatus.Complete
            || status == AgvTaskStatus.Cancel
            || status == AgvTaskStatus.ExceptionComplete;
    }

    private static string GetTaskStatusText(AgvTaskStatus status)
    {
        switch (status)
        {
            case AgvTaskStatus.Created: return "创建任务";
            case AgvTaskStatus.WaitingExecuting: return "等待执行";
            case AgvTaskStatus.Executing: return "执行中";
            case AgvTaskStatus.TaskStart: return "任务开始";
            case AgvTaskStatus.CellOut: return "出储位";
            case AgvTaskStatus.WaitingContinue: return "等待任务继续";
            case AgvTaskStatus.WaitingContinueResponse: return "等待继续任务响应";
            case AgvTaskStatus.ContinueExecuting: return "继续执行";
            case AgvTaskStatus.WaitingCancelResponse: return "等待取消响应";
            case AgvTaskStatus.Complete: return "任务完成";
            case AgvTaskStatus.Cancel: return "已取消";
            case AgvTaskStatus.Error: return "设备错误";
            case AgvTaskStatus.ExceptionComplete: return "异常完成";
            default: return status.ToString();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("机台状态监控服务停止");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    public class MachineConfig
    {
        public string MachineName { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; } = 502;
        public byte SlaveId { get; set; } = 10;

        public ushort Gate1FullMaterialAddress { get; set; }
        public string Gate1CellCode { get; set; }
        public string Gate1TaskType { get; set; }
        public ushort Gate2FullMaterialAddress { get; set; }
        public string Gate2CellCode { get; set; }
        public string Gate2TaskType { get; set; }
        public string MachineCellCode { get; set; }
    }
}
