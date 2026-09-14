using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks.Dtos;
using TuTa.Wms.Machines.Aggregates;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp;
using Wms.ModbusTool;
using Newtonsoft.Json;
using TuTa.Wms.AgvTasks.Aggregaes;

namespace TuTa.Wms.AgvTasks
{
 
    public class AgvTaskService : WmsAppService, IAgvTaskService
    {
        private ILogger<AgvTaskService> _logger;
        private AgvTaskManager _agvTaskManager;
        private readonly ModbusHelper _modbusHelper;
        private readonly IRepository<Machine, int> _machineRepository;
        private readonly IAgvTaskRepository _agvTaskRepository;
        private readonly IRepository<MachinePoint, int> _machinePointRepository;
        private readonly UnitOfWorkManager _unitOfWorkManager;
        private readonly RcsApiManager _rcsApiManager;
        private readonly AgvTaskMonitorManager _monitorManager;

        /// <summary>
        /// 机台监控线程最长监控时长，超时后自动结束，避免机台无响应时线程常驻
        /// </summary>
        private static readonly TimeSpan MonitorTimeout = TimeSpan.FromMinutes(30);

        public AgvTaskService(
            ILogger<AgvTaskService> logger
            , AgvTaskManager agvTaskManager
            , ModbusHelper modbusHelper
            , IRepository<Machine, int> machineRepository
            , IAgvTaskRepository agvTaskRepository
            , IRepository<MachinePoint, int> machinePointRepository
            , UnitOfWorkManager unitOfWorkManager
            , RcsApiManager rcsApiManager
            , AgvTaskMonitorManager monitorManager)
        {
            _logger = logger;
            _agvTaskManager = agvTaskManager;
            _modbusHelper = modbusHelper;
            _machineRepository = machineRepository;
            _agvTaskRepository = agvTaskRepository;
            _machinePointRepository = machinePointRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _rcsApiManager = rcsApiManager;
            _monitorManager = monitorManager;
        }

        /// <summary>
        /// CTU任务回调
        /// </summary>
        /// <param name="reqCode"></param>
        /// <returns></returns>
        [UnitOfWork]
        public async Task<ResultAgvTaskDto> CtuCallbackAsync(AgvCallBackRequest input)
        {
            _logger.LogInformation($"收到CTU回调，请求参数:{JsonConvert.SerializeObject(input)}");
            if (input.Method == "taskStart")
            {
                try
                {
                    var agvTask = await _agvTaskManager.SetAsTaskStart(input.TaskCode);
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                catch (Exception e)
                {
                    return new ResultAgvTaskDto("1", e.Message, input.ReqCode, "");
                }

            }
            // 处理申请放逻辑
            else if (input.Method == "boxApplyPassPut")
            {
                try
                {
                    // 处理申请放逻辑
                    var agvTask = await _agvTaskManager.SetAsTaskArrive(input.TaskCode);

                    // 根据任务类型判断是否需要和机台交互
                    if (agvTask.StockTyp == ManageType.EmptyBoxToMachine ||
                        agvTask.StockTyp == ManageType.SemiFinishedFromMachine)
                    {
                        // 需要和机台交互
                        // 在进入后台任务之前，先查询机台站点信息
                        var machinePoint = await GetMachinePointByCellCodeAsync(agvTask.StartPositionCode);
                        if (machinePoint == null)
                        {
                            machinePoint = await GetMachinePointByCellCodeAsync(agvTask.EndPositionCode);
                        }

                        if (machinePoint == null)
                        {
                            _logger.LogError($"未找到对应的机台站点信息，任务编号: {agvTask.ReqCode}");
                            return new ResultAgvTaskDto("1", "未找到对应的机台站点信息", input.ReqCode, "");
                        }
            
                        // 启动后台任务持续监听机台响应
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // 持续监听，直到收到机台响应、任务取消或超时
                                await ContinuouslyMonitorMachineAsync(input, agvTask, machinePoint, "2");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "持续监听机台响应失败");
                            }
                        });
                    }
                    else
                    {
                        // 不需要和机台交互，直接调用料箱取放接口
                        _logger.LogInformation($"任务类型 {agvTask.StockTyp} 不需要和机台交互，直接调用料箱取放接口");
                        await _agvTaskManager.BoxApplyPassAsync(input.TaskCode, "2");
                    }

                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                catch (Exception e)
                {
                    return new ResultAgvTaskDto("1", e.Message, input.ReqCode, "");
                }

            }
            // 处理申请取逻辑
            else if (input.Method == "boxApplyPassGet")
            {
                try
                {
                    // 处理申请取逻辑
                    var agvTask = await _agvTaskManager.SetAsTaskArrive(input.TaskCode);

                    // 根据任务类型判断是否需要和机台交互
                    if (agvTask.StockTyp == ManageType.FinishedFromMachine || 
                        agvTask.StockTyp == ManageType.EmptyBoxFromMachine)
                    {
                        // 需要和机台交互
                        // 在进入后台任务之前，先查询机台站点信息
                        var machinePoint = await GetMachinePointByCellCodeAsync(agvTask.StartPositionCode);
                        if (machinePoint == null)
                        {
                            machinePoint = await GetMachinePointByCellCodeAsync(agvTask.EndPositionCode);
                        }

                        if (machinePoint == null)
                        {
                            _logger.LogError($"未找到对应的机台站点信息，任务编号: {agvTask.ReqCode}");
                            return new ResultAgvTaskDto("1", "未找到对应的机台站点信息", input.ReqCode, "");
                        }
            
                        // 启动后台任务持续监听机台响应
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // 持续监听，直到收到机台响应、任务取消或超时
                                await ContinuouslyMonitorMachineAsync(input, agvTask, machinePoint, "1");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "持续监听机台响应失败");
                            }
                        });
                    }
                    else
                    {
                        // 不需要和机台交互，直接调用料箱取放接口
                        _logger.LogInformation($"任务类型 {agvTask.StockTyp} 不需要和机台交互，直接调用料箱取放接口");
                        await _agvTaskManager.BoxApplyPassAsync(input.TaskCode, "1");
                    }

                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                catch (Exception e)
                {
                    return new ResultAgvTaskDto("1", e.Message, input.ReqCode, "");
                }

            }
            else if (input.Method == "cellOut")
            {
                try
                {
                    // 处理出储位逻辑
                    _logger.LogInformation($"收到出储位回调，任务编号: {input.TaskCode}，当前位置: {input.CurrentPositionCode}");

                    // 处理到位逻辑
                    var agvTask = await _agvTaskManager.SetAsCellOut(input.TaskCode);

                    // 从机台站点表读取信息
                    var machinePoint = await GetMachinePointByCellCodeAsync(agvTask.StartPositionCode);
                    if (machinePoint == null)
                    {
                                machinePoint = await GetMachinePointByCellCodeAsync(agvTask.EndPositionCode);
                    }
                    // 启动后台任务处理 Modbus 通信（收回操作）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            
                            if (machinePoint != null)
                            {
                                // 根据任务类型判断是否需要处理收回操作
                                if (agvTask.StockTyp == ManageType.FinishedFromMachine || 
                                    agvTask.StockTyp == ManageType.EmptyBoxFromMachine)
                                {
                                    // 收尾信号序列：到位信号清0 → 完成信号置1 → 延时2秒 → 完成信号清0（每步重试直到成功，最多10次）
                                    await FinishMachineSignalSequenceAsync(machinePoint, input.TaskCode);
                                }
                                else
                                {
                                    _logger.LogInformation($"任务类型 {agvTask.StockTyp} 不需要处理收回操作");
                                }
                            }
                            else
                            {
                                _logger.LogError($"未找到对应的机台站点信息，当前任务: {input.TaskCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "处理出储位 Modbus 通信失败");
                        }
                    });

                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "处理出储位回调失败");
                    return new ResultAgvTaskDto("1", e.Message, input.ReqCode, "");
                }

            }
            else if (input.Method == "taskFinish")
            {
                try
                {
                    var agvTask = await _agvTaskManager.SetAsCompletedAsync(input.TaskCode);

                    // 任务已完成，结束对应的机台监控线程（若仍在监听）
                    bool monitorCancelled = _monitorManager.TryCancel(agvTask.ReqCode) | _monitorManager.TryCancel(input.TaskCode);
                    if (monitorCancelled)
                    {
                        _logger.LogInformation($"任务完成 - 已结束机台监控线程，任务编号: {input.TaskCode}");
                    }

                    // 从机台站点表读取信息
                    var machinePoint = await GetMachinePointByCellCodeAsync(agvTask.StartPositionCode);
                    if (machinePoint == null)
                    {
                        machinePoint = await GetMachinePointByCellCodeAsync(agvTask.EndPositionCode);
                    }
                    // 启动后台任务处理 Modbus 通信
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (machinePoint != null)
                            {
                                // 根据任务类型判断是否需要处理完成操作
                                if (agvTask.StockTyp == ManageType.EmptyBoxToMachine || 
                                    agvTask.StockTyp == ManageType.SemiFinishedFromMachine)
                                {
                                    // 收尾信号序列：到位信号清0 → 完成信号置1 → 延时2秒 → 完成信号清0（每步重试直到成功，最多10次）
                                    await FinishMachineSignalSequenceAsync(machinePoint, input.TaskCode);
                                }
                                else
                                {
                                    _logger.LogInformation($"任务类型 {agvTask.StockTyp} 不需要处理完成操作");
                                }
                            }
                            else
                            {
                                _logger.LogError($"未找到对应的机台站点信息，当前任务: {input.TaskCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "处理出储位 Modbus 通信失败");
                        }
                    });
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                catch (Exception e)
                {
                    return new ResultAgvTaskDto("1", e.Message, input.ReqCode, "");

                }

            }
            else if (input.Method == "taskCancel")
            {
                try
                {
                    AgvTask agvTask;
                    try
                    {
                        agvTask = await _agvTaskManager.SetAsCancelAsync(input.TaskCode,false);
                    }
                    catch (UserFriendlyException ex)
                    {
                        // 界面主动取消后本地已是终态，RCS 再回调 taskCancel 时会走到这里；
                        // 不能直接返回失败，否则到位信号清零逻辑执行不到，会造成信号残留
                        _logger.LogWarning($"任务取消回调 - 本地已是终态: {ex.Message}，任务编号: {input.TaskCode}，继续执行信号清理");
                        agvTask = await _agvTaskManager.FindByReqCodeAsync(input.TaskCode);
                        if (agvTask == null)
                        {
                            return new ResultAgvTaskDto("0", "任务不存在", input.ReqCode, "");
                        }
                    }

                    // 取消正在进行的监控线程（通过单例管理器跨请求查找）
                    bool monitorCancelled = _monitorManager.TryCancel(agvTask.ReqCode) | _monitorManager.TryCancel(input.TaskCode);
                    if (monitorCancelled)
                    {
                        _logger.LogInformation($"任务取消 - 已取消机台监控线程，任务编号: {input.TaskCode}");
                    }
                    else
                    {
                        _logger.LogInformation($"任务取消 - 未找到正在进行的机台监控线程，任务编号: {input.TaskCode}");
                    }

                    // 启动后台任务处理 Modbus 通信，清除到位信号
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // 从机台站点表读取信息
                            var machinePoint = await GetMachinePointByCellCodeAsync(agvTask.EndPositionCode);
                            if (machinePoint == null)
                            {
                                machinePoint = await GetMachinePointByCellCodeAsync(agvTask.StartPositionCode);
                            }

                            if (machinePoint != null)
                            {   
                                    // 获取通信参数
                                    string machineIp = machinePoint.IpAddress;
                                    int port = machinePoint.Port;
                                    byte slaveId = machinePoint.SlaveId;

                                    // 写入到位信号为 0（重试直到成功，最多10次）
                                    await TryWriteMachineSignalWithRetryAsync(machineIp, port, slaveId, machinePoint.ArriveAddress, false, "任务取消-到位信号清0", input.TaskCode);

                            }
                            else
                            {
                                _logger.LogError($"任务取消 - 未找到对应的机台站点信息，当前任务: {input.TaskCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "任务取消 - 处理 Modbus 通信失败");
                        }
                    });
                    
                    return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
                }
                catch (Exception e)
                {
                    return new ResultAgvTaskDto("1", e.Message, input.ReqCode, "");
                }
            }
            else
            {
                return new ResultAgvTaskDto("0", "成功", input.ReqCode, "");
            }
        }

        public async Task<PagedResultDto<AgvTaskDto>> GetPagingListAsync(PagingAgvTaskListInput input)
        {
            var agvTasks = await _agvTaskManager.GetPagingListAsync(
                input.Filter, input.StartCreationTime, input.EndCreationTime, input.AgvTaskStatus,
                input.SkipCount, input.PageSize, "CreationTime desc");

            var totalCount = await _agvTaskManager.GetPagingCountAsync(
                input.Filter, input.StartCreationTime, input.EndCreationTime, input.AgvTaskStatus);

            var agvTaskDtos = agvTasks.Select(task => new AgvTaskDto
            {
                Id = task.Id,
                StartPosition = task.StartPositionCode,
                TargetPosition = task.EndPositionCode,
                BoxCode = task.BoxCode,
                TaskType = task.TaskTyp,
                TaskStatus = (int)task.AgvTaskStatus,
                CreateTime = task.CreationTime,
                StockTyp = (int)task.StockTyp
            }).ToList();

            return new PagedResultDto<AgvTaskDto>(totalCount, agvTaskDtos);
        }

        public async Task SetAsExecutingAsync(int taskId)
        {
            var agvTask = await _agvTaskManager.FindByIdAsync(taskId);
            if (agvTask == null)
            {
                throw new UserFriendlyException("任务不存在");
            }
            await _agvTaskManager.SetAsExecutingAsync(agvTask);
        }

        public async Task SetAsCompletedAsync(int taskId)
        {
            var agvTask = await _agvTaskManager.FindByIdAsync(taskId);
            if (agvTask == null)
            {
                throw new UserFriendlyException("任务不存在");
            }
            if (agvTask.AgvTaskStatus == AgvTaskStatus.Complete)
            {
                throw new UserFriendlyException("任务已完成，不能重复完成");
            }
            if (agvTask.AgvTaskStatus == AgvTaskStatus.Cancel)
            {
                throw new UserFriendlyException("任务已取消，不能完成");
            }
            await _agvTaskManager.SetAsCompletedAsync(agvTask.ReqCode);
        }

        public async Task SetAsCancelAsync(int taskId)
        {
            var agvTask = await _agvTaskManager.FindByIdAsync(taskId);
            if (agvTask == null)
            {
                throw new UserFriendlyException("任务不存在");
            }
            if (agvTask.AgvTaskStatus == AgvTaskStatus.Complete)
            {
                throw new UserFriendlyException("任务已完成，不能取消");
            }
            if (agvTask.AgvTaskStatus == AgvTaskStatus.Cancel)
            {
                throw new UserFriendlyException("任务已取消，不能重复取消");
            }
            await _agvTaskManager.SetAsCancelAsync(agvTask.ReqCode);

            // 本地取消成功（RCS 已确认取消），结束可能还在等待机台响应的监控线程
            // 到位信号清零由 RCS 随后的 taskCancel 回调完成（该回调对已取消任务已做容错）
            if (_monitorManager.TryCancel(agvTask.ReqCode))
            {
                _logger.LogInformation($"界面取消任务 - 已结束机台监控线程，任务编号: {agvTask.ReqCode}");
            }
        }

        /// <summary>
        /// 持续监听机台响应
        /// </summary>
        /// <param name="input">回调输入</param>
        /// <param name="agvTask">AGV任务</param>
        /// <param name="machineParams">机台监控参数（已在外部查询好）</param>
        /// <param name="passType">料箱取放类型</param>
        /// <returns></returns>
        private async Task ContinuouslyMonitorMachineAsync(AgvCallBackRequest input, AgvTask agvTask, MachinePoint machineParams, string passType)
        {
            // 检查 agvTask 是否为 null
            if (agvTask == null)
            {
                _logger.LogError("agvTask 为 null，无法执行持续监听");
                return;
            }

            // 检查参数是否为 null
            if (machineParams == null)
            {
                _logger.LogError("machineParams 为 null，无法执行持续监听");
                return;
            }

            string taskCode = agvTask.ReqCode;

            // 注册到单例管理器：跨请求可被 taskCancel / taskFinish 取消，且超时后自动结束
            var cts = _monitorManager.Register(taskCode, MonitorTimeout);
            var token = cts.Token;

            try
            {
                _logger.LogInformation($"开始持续监听机台响应，任务编号: {agvTask.ReqCode}");

                // 先复位完成信号，清掉上一轮任务的残留，避免机台状态机误判（写入失败自动重试直到成功）
                await WriteMachineSignalWithRetryAsync(machineParams, machineParams.CompleteAddress, false, "完成信号复位", taskCode, token);
                // 再写入到位信号，保证干净的上升沿触发机台流程（写入失败自动重试直到成功）
                await WriteMachineSignalWithRetryAsync(machineParams, machineParams.ArriveAddress, true, "到位信号", taskCode, token);
                // 监听间隔（毫秒）
                int monitorInterval = 1000;

                // 持续监听治具伸出信号
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // 读取治具伸出信号
                        bool jigExtendStatus = await _modbusHelper.ReadCoilAsync(machineParams.IpAddress, machineParams.Port, machineParams.SlaveId, machineParams.JigExtendAddress);

                        // 处理治具伸出信号
                        if (jigExtendStatus)
                        {
                            _logger.LogInformation($"收到治具伸出信号，地址: {machineParams.JigExtendAddress}, 任务编号: {agvTask.ReqCode}");

                            // 使用新的工作单元来避免并发问题
                            using (var uow = _unitOfWorkManager.Begin())
                            {
                                // 调用料箱取放接口
                                await _agvTaskManager.BoxApplyPassAsync(agvTask.ReqCode, passType);
                                _logger.LogInformation($"调用料箱取放接口，任务编号: {agvTask.ReqCode}，类型: {passType}");
                                await uow.CompleteAsync();
                            }

                            // 停止监听
                            break;
                        }

                        // 等待一段时间后再次监听
                        await Task.Delay(monitorInterval, token);
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消或超时，正常退出
                        _logger.LogInformation($"机台监听任务已结束（取消或超时），任务编号: {agvTask.ReqCode}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "监听治具伸出信号失败");
                        // 短暂延迟后继续监听
                        try
                        {
                            await Task.Delay(5000, token);
                        }
                        catch (OperationCanceledException)
                        {
                            // 任务被取消，正常退出
                            _logger.LogInformation($"机台监听任务已结束（取消或超时），任务编号: {agvTask.ReqCode}");
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，正常退出
                _logger.LogInformation($"机台监听任务已结束（取消或超时），任务编号: {agvTask.ReqCode}");
            }
            catch (Volo.Abp.Data.AbpDbConcurrencyException ex)
            {
                // 处理乐观并发冲突
                _logger.LogError(ex, $"乐观并发冲突，任务编号: {agvTask.ReqCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "持续监听机台响应异常");
            }
            finally
            {
                // 从监控任务管理器中移除并释放令牌
                _monitorManager.Unregister(taskCode, cts);
                _logger.LogInformation($"结束持续监听机台响应，任务编号: {agvTask.ReqCode}");
            }
        }

        /// <summary>
        /// 写入机台信号并重试，直到写入成功或监控线程被取消（取消/超时后抛出 OperationCanceledException 正常退出）
        /// </summary>
        /// <param name="machineParams">机台站点信息</param>
        /// <param name="address">信号地址</param>
        /// <param name="value">写入值</param>
        /// <param name="signalName">信号名称（用于日志）</param>
        /// <param name="taskCode">任务编号（用于日志）</param>
        /// <param name="token">取消令牌</param>
        private async Task WriteMachineSignalWithRetryAsync(MachinePoint machineParams, ushort address, bool value, string signalName, string taskCode, CancellationToken token)
        {
            const int retryDelayMs = 2000;
            int attempt = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                attempt++;

                try
                {
                    bool success = await _modbusHelper.WriteMachineCommandAsync(machineParams.IpAddress, machineParams.Port, machineParams.SlaveId, address, value);
                    if (success)
                    {
                        _logger.LogInformation($"{signalName}写入成功{(attempt > 1 ? $"（第 {attempt} 次尝试）" : "")}，地址: {address}, 任务编号: {taskCode}");
                        return;
                    }
                    _logger.LogWarning($"{signalName}第 {attempt} 次写入失败（返回 false），{retryDelayMs / 1000} 秒后重试，地址: {address}, 任务编号: {taskCode}");
                }
                catch (OperationCanceledException)
                {
                    // 取消令牌触发，向上抛出由监控方法的取消分支正常退出
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"{signalName}第 {attempt} 次写入异常，{retryDelayMs / 1000} 秒后重试，地址: {address}, 任务编号: {taskCode}");
                }

                await Task.Delay(retryDelayMs, token);
            }
        }

        /// <summary>
        /// 写入机台信号并重试（回调后台任务场景，无取消令牌），达到最大重试次数仍失败返回 false
        /// </summary>
        /// <param name="ipAddress">机台 IP</param>
        /// <param name="port">端口</param>
        /// <param name="slaveId">从站号</param>
        /// <param name="address">信号地址</param>
        /// <param name="value">写入值</param>
        /// <param name="signalName">信号名称（用于日志）</param>
        /// <param name="taskCode">任务编号（用于日志）</param>
        /// <param name="maxRetry">最大尝试次数</param>
        private async Task<bool> TryWriteMachineSignalWithRetryAsync(string ipAddress, int port, byte slaveId, ushort address, bool value, string signalName, string taskCode, int maxRetry = 10)
        {
            const int retryDelayMs = 2000;

            for (int attempt = 1; attempt <= maxRetry; attempt++)
            {
                try
                {
                    if (await _modbusHelper.WriteMachineCommandAsync(ipAddress, port, slaveId, address, value))
                    {
                        _logger.LogInformation($"{signalName}写入成功{(attempt > 1 ? $"（第 {attempt} 次尝试）" : "")}，地址: {address}, 任务编号: {taskCode}");
                        return true;
                    }
                    _logger.LogWarning($"{signalName}第 {attempt}/{maxRetry} 次写入失败（返回 false），{retryDelayMs / 1000} 秒后重试，地址: {address}, 任务编号: {taskCode}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"{signalName}第 {attempt}/{maxRetry} 次写入异常，{retryDelayMs / 1000} 秒后重试，地址: {address}, 任务编号: {taskCode}");
                }

                if (attempt < maxRetry)
                {
                    await Task.Delay(retryDelayMs);
                }
            }

            _logger.LogError($"{signalName}重试 {maxRetry} 次后仍写入失败，地址: {address}, 任务编号: {taskCode}");
            return false;
        }

        /// <summary>
        /// 任务收尾的机台信号序列：到位信号清0 → 完成信号置1 → 延时2秒 → 完成信号清0
        /// 每一步都重试直到成功（最多10次），某一步最终失败则中止后续步骤并记录错误
        /// </summary>
        /// <param name="machinePoint">机台站点信息</param>
        /// <param name="taskCode">任务编号（用于日志）</param>
        private async Task FinishMachineSignalSequenceAsync(MachinePoint machinePoint, string taskCode)
        {
            string machineIp = machinePoint.IpAddress;
            int port = machinePoint.Port;
            byte slaveId = machinePoint.SlaveId;

            // 1. 到位信号清 0
            if (!await TryWriteMachineSignalWithRetryAsync(machineIp, port, slaveId, machinePoint.ArriveAddress, false, "到位信号清0", taskCode))
            {
                _logger.LogError($"到位信号清0最终失败，跳过后续完成信号脉冲，任务编号: {taskCode}");
                return;
            }

            // 2. 完成信号置 1
            if (!await TryWriteMachineSignalWithRetryAsync(machineIp, port, slaveId, machinePoint.CompleteAddress, true, "完成信号置1", taskCode))
            {
                _logger.LogError($"完成信号置1最终失败，跳过完成信号清0，任务编号: {taskCode}");
                return;
            }

            // 3. 保持 2 秒后清 0 完成信号
            await Task.Delay(2000);
            await TryWriteMachineSignalWithRetryAsync(machineIp, port, slaveId, machinePoint.CompleteAddress, false, "完成信号清0", taskCode);
        }

        /// <summary>
        /// 根据CellCode获取机台站点信息
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <returns></returns>
        private async Task<MachinePoint> GetMachinePointByCellCodeAsync(string cellCode)
        {
            try
            {
                // 使用独立的工作单元，避免 DbContext 并发问题
                using (var uow = _unitOfWorkManager.Begin())
                {
                    // 直接在数据库中根据CellCode查询，提高性能
                    var machinePoints = await _machinePointRepository.GetListAsync(mp => mp.CellCode == cellCode);
                    var result = machinePoints.FirstOrDefault();
                    
                    await uow.CompleteAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询机台站点信息失败");
                return null;
            }
        }

        /// <summary>
        /// 空盒衬上机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <returns></returns>
        public async Task CreateEmptyBoxToMachineTaskAsync(string cellCode)
        {
            try
            {
                _logger.LogInformation($"创建空盒衬上机台任务，结束库位: {cellCode}");
                await _agvTaskManager.CreateEmptyBoxLiningTaskAsync(cellCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建空盒衬上机台任务失败: {ex.Message}");
                throw new UserFriendlyException(ex.Message);
            }
        }

        /// <summary>
        /// 成品下机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <param name="machineName">机台名称</param>
        /// <returns></returns>
        public async Task CreateFinishedFromMachineTaskAsync(string cellCode, string machineName)
        {
            try
            {
                _logger.LogInformation($"创建成品下机台任务，开始库位: {cellCode}，机台名称: {machineName}");
                await _agvTaskManager.CreateFinishedFromMachineTaskAsync(cellCode, machineName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建成品下机台任务失败: {ex.Message}");
                throw new UserFriendlyException(ex.Message);
            }
        }

        /// <summary>
        /// 空盒衬下机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <returns></returns>
        public async Task CreateEmptyBoxFromMachineTaskAsync(string cellCode)
        {
            try
            {
                _logger.LogInformation($"创建空盒衬下机台任务，开始库位: {cellCode}");
                await _agvTaskManager.CreateEmptyBoxFromMachineTaskAsync(cellCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建空盒衬下机台任务失败: {ex.Message}");
                throw new UserFriendlyException(ex.Message);
            }
        }

        /// <summary>
        /// 半成品上料任务（半成品加工上料）
        /// </summary>
        /// <param name="cellCode">结束库位编码（机台库位）</param>
        /// <param name="machineName">机台名称</param>
        /// <returns></returns>
        public async Task CreateSemiFinishedFromMachineTaskAsync(string cellCode, string machineName)
        {
            try
            {
                _logger.LogInformation($"创建半成品上料任务，结束库位: {cellCode}，机台名称: {machineName}");
                await _agvTaskManager.CreateSemiFinishedFromMachineTaskAsync(cellCode, machineName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建半成品上料任务失败: {ex.Message}");
                throw new UserFriendlyException(ex.Message);
            }
        }
    }
}
