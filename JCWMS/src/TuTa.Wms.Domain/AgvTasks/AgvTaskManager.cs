using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks.Aggregaes;
using TuTa.Wms.Cells.Aggregates;
using TuTa.Wms.Cells;
using Volo.Abp.Uow;
using Volo.Abp;
using Microsoft.Extensions.Logging;
using Wms.LogTool;
using TuTa.Wms.Boxes;
using TuTa.Wms.Warehouses;
using TuTa.Wms.Boxes.Aggregates;
using TuTa.Wms.Warehouses.Aggregates;
using TuTa.Wms.Warehouses.Entities;
using TuTa.Wms.Stocks;
using TuTa.Wms.Stocks.Aggregates;
using TuTa.Wms.ChkResultLists;
using TuTa.Wms.ChkResultLists.Aggregates;
using TuTa.Wms.Stocks.ValueObjects;
using TuTa.Wms.Skips.Aggregates;
using TuTa.Wms.Skips;
using TuTa.Wms.PickLists;
using TuTa.Wms.BarcodeLists;
using Volo.Abp.EventBus.Local;
using TuTa.Wms.Stocks.Events;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Identity;
using TuTa.Wms.PickLists.Events;
using TuTa.Wms.PickLists.Aggregates;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TuTa.Wms.Machines.Aggregates;
using Volo.Abp.Domain.Repositories;

namespace TuTa.Wms.AgvTasks
{
    public class AgvTaskManager : WmsDomainService
    {
        private readonly IAgvTaskRepository _agvTaskRepository;
        private readonly RcsApiManager _rcsApiManager;
        private readonly IBoxRepository _boxRepository;
        private readonly ICellRepository _cellRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IChkResultListRepository _chkResultListRepository;
        private readonly IBarcodeCheckRepository _barcodeCheckRepository;
        private readonly ISkipRepository _skipRepository;
        private readonly IPickListRepository _pickListRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IdentityUserManager _userManager;
        private readonly PickListManager _pickListManager;
        private readonly AGVOptions _aGVOptions;
        private readonly LocalEventBus _localEventBus;
        private readonly ILogger<AgvTaskManager> _logger;
        private readonly IRepository<Machine, int> _machineRepository;
        private readonly IRepository<MachinePoint, int> _machinePointRepository;
        public AgvTaskManager(IAgvTaskRepository agvTaskRepository,
            RcsApiManager rcsApiManager
            , ICellRepository cellRepository
            , IWarehouseRepository warehouseRepository
            , IBoxRepository boxRepository
            , IStockRepository stockRepository
            , IChkResultListRepository chkResultListRepository
            , IBarcodeCheckRepository barcodeCheckRepository
            , ISkipRepository skipRepository
            , IPickListRepository pickListRepository
            , IRepository<Machine, int> machineRepository
            , IRepository<MachinePoint, int> machinePointRepository
            , IOptionsSnapshot<AGVOptions> aGVOptions
            , IUnitOfWorkManager unitofWorkManager
            , IdentityUserManager userManager
            , PickListManager pickListManager
            , LocalEventBus localEventBus
            , ILogger<AgvTaskManager> logger

            )
        {
            _agvTaskRepository = agvTaskRepository;
            _cellRepository = cellRepository;
            _rcsApiManager = rcsApiManager;
            _boxRepository = boxRepository;
            _warehouseRepository = warehouseRepository;
            _stockRepository = stockRepository;
            _chkResultListRepository = chkResultListRepository;
            _barcodeCheckRepository = barcodeCheckRepository;
            _skipRepository = skipRepository;
            _pickListRepository = pickListRepository;
            _machineRepository = machineRepository;
            _machinePointRepository = machinePointRepository;
            _userManager = userManager;
            _pickListManager = pickListManager;
            _aGVOptions = aGVOptions.Value;
            _localEventBus = localEventBus;
            _logger = logger;
        }

        /// <summary>
        /// 同一物理仓位编码在不同机台下的记录共享运行状态，并按取放结果同步显式指定的有无货状态。
        /// 在调用方工作单元中统一持久化，不复制物料、架子名称等机台关联字段。
        /// </summary>
        /// <param name="anchor">本次任务选中的库位；使用其主编码同步，兼容通过备用编码找到库位的取消流程。</param>
        /// <param name="status">Selected 表示锁定，Enable 表示解除锁定。</param>
        /// <param name="cellStatus">取放完成后确定的有无货状态；锁定或取消时不传，保留各记录原有状态。</param>
        public async Task SetSharedCellRunStatusAsync(Cell anchor, CellRunStatus status, CellStatus? cellStatus = null)
        {
            if (status != CellRunStatus.Selected && status != CellRunStatus.Enable)
                throw new ArgumentOutOfRangeException(nameof(status));

            var cells = await _cellRepository.GetListAsync(cell => cell.CellCode == anchor.CellCode);
            if (status == CellRunStatus.Selected && cells.Any(cell => cell.RunStatus == CellRunStatus.Selected))
                throw new UserFriendlyException($"仓位{anchor.CellCode}已被锁定，不能重复下发任务");

            foreach (var member in cells)
            {
                // 只按本次业务明确给出的状态更新整组，不能从第一条记录推断其余记录的有无货。
                var cell = member.Id == anchor.Id ? anchor : member;
                if (cellStatus.HasValue)
                    cell.SetCellStatus(cellStatus.Value);
                if (status == CellRunStatus.Selected)
                    cell.SetSelected();
                else
                    cell.SetEnable();
                await _cellRepository.UpdateAsync(cell);
            }
        }

        /// <summary>
        /// 按候选顺序选择整组均未锁定的物理仓位，兼容历史数据中同编码锁定状态不一致的情况。
        /// </summary>
        /// <param name="candidates">已按区域、机台及有无货筛选的库位，保留调用方的优先顺序。</param>
        /// <returns>第一个同编码记录均未锁定的候选；无可用仓位时返回 null。</returns>
        internal async Task<Cell> FindUnlockedSharedCellAsync(List<Cell> candidates)
        {
            if (candidates.Count == 0)
                return null;

            var codes = candidates.Select(cell => cell.CellCode).Distinct().ToList();
            var lockedCells = await _cellRepository.GetListAsync(cell =>
                codes.Contains(cell.CellCode) && cell.RunStatus == CellRunStatus.Selected);
            var lockedCodes = lockedCells.Select(cell => cell.CellCode).ToHashSet();
            return candidates.FirstOrDefault(cell => !lockedCodes.Contains(cell.CellCode));
        }

        /// <summary>为下空盒衬任务选择最低的可用空库位。同层按库位编码排序，无法识别层号的旧库位排在最后。</summary>
        /// <param name="candidates">已按机台、货架及空位状态筛选的候选库位。</param>
        /// <returns>第一个未被共享锁占用的库位，无可用库位时返回 null。</returns>
        internal Task<Cell> FindLowestUnlockedShelfCellAsync(List<Cell> candidates)
        {
            return FindUnlockedSharedCellAsync(candidates
                .OrderBy(cell => GetShelfLayer(cell.CellCode))
                .ThenBy(cell => cell.CellCode, StringComparer.Ordinal)
                .ToList());
        }

        /// <summary>成品按列号升序放置，同列从底层往上选位，无法识别层号的旧库位排在最后。</summary>
        /// <param name="candidates">已按机台、成品货架及空位状态筛选的候选库位。</param>
        /// <returns>按放置顺序找到的第一个未锁定的库位，无可用库位时返回 null。</returns>
        internal Task<Cell> FindUnlockedFinishedShelfCellAsync(List<Cell> candidates)
        {
            return FindUnlockedSharedCellAsync(candidates
                .Select(cell => (Cell: cell, Layer: GetShelfLayer(cell.CellCode)))
                .OrderBy(candidate => candidate.Layer == int.MaxValue)
                .ThenBy(candidate => candidate.Layer == int.MaxValue ? null : candidate.Cell.CellCode[..6], StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Layer)
                .ThenBy(candidate => candidate.Cell.CellCode, StringComparer.Ordinal)
                .Select(candidate => candidate.Cell)
                .ToList());
        }

        /// <summary>从三层货架点位编码（如 600001X3501013）读取第十至十一位的层号，01 为底层。</summary>
        /// <param name="code">物理库位编码。</param>
        /// <returns>有效层号为 1 至 3，无法识别时返回 int.MaxValue。</returns>
        private static int GetShelfLayer(string code)
        {
            if (code?.Length == 14 && code[6] == 'X' && char.IsAsciiDigit(code[7]) &&
                char.IsAsciiDigit(code[8]) && code[9] == '0' && code[10] is >= '1' and <= '3' &&
                code.EndsWith("013", StringComparison.Ordinal))
            {
                return code[10] - '0';
            }

            return int.MaxValue;
        }

        [UnitOfWork]
        public async Task<AgvTask> CreateCtuStockInByStockTaskAsync(string boxCode, string ctnrTyp, string startCellName, string endCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.CTUTaskType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName + "${05}";//按照仓位下达任务
            userCallCodePath[1] = endCellName + "${05}";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, boxCode,
            startCellName, endCellName, ctnrTyp, type);
            var result = await _agvTaskRepository.InsertAsync(entity);


            await SetAsExecutingAsync(entity);

            return result;
        }

        [UnitOfWork]
        public async Task<AgvTask> CreateCtuSSXTaskAsync(string boxCode, string ctnrTyp, string startCellName, string endCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.CTUTaskXianType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName + "${05}";//按照仓位下达任务
            userCallCodePath[1] = endCellName + "${05}";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, boxCode,
            startCellName, endCellName, ctnrTyp, type);
            var result = await _agvTaskRepository.InsertAsync(entity);


            await SetAsExecutingAsync(entity);

            return result;
        }

        [UnitOfWork]
        public async Task<AgvTask> CreateLiftStockInByStockTaskAsync(string boxCode, string ctnrTyp, string startCellName, string endCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.LiftTaskType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName + "${05}";//按照仓位下达任务
            userCallCodePath[1] = endCellName + "${05}";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, boxCode,
            startCellName, endCellName, ctnrTyp, type);
            var result = await _agvTaskRepository.InsertAsync(entity);

            await SetAsExecutingAsync(entity);

            return result;
        }

        [UnitOfWork]
        public async Task<AgvTask> CreateLiftSSXTaskAsync(string boxCode, string ctnrTyp, string startCellName, string endCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.LiftTaskXianType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName + "${05}";//按照仓位下达任务
            userCallCodePath[1] = endCellName + "${05}";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, boxCode,
            startCellName, endCellName, ctnrTyp, type);
            var result = await _agvTaskRepository.InsertAsync(entity);

            await SetAsExecutingAsync(entity);

            return result;
        }

        [UnitOfWork]
        public async Task<AgvTask> CreateSkipTaskAsync(string startCellName, string endCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            string taskTyp = "";
            string[] userCallCodePath = null;
            if (type == ManageType.SkipSend)
            {
                taskTyp = _aGVOptions.SkipSendType;
                userCallCodePath = new string[3];
                userCallCodePath[0] = startCellName;//按照仓位下达任务
                userCallCodePath[1] = endCellName;//按照仓位下达任务
                userCallCodePath[2] = "1";
            }
            else if (type == ManageType.SkipCall)
            {
                taskTyp = _aGVOptions.SkipCallType;
                userCallCodePath = new string[3];
                userCallCodePath[0] = "1";
                userCallCodePath[1] = startCellName;//按照仓位下达任务
                userCallCodePath[2] = endCellName;//按照仓位下达任务
            }
            else
            {
                taskTyp = _aGVOptions.SkipTaskType;
                userCallCodePath = new string[2];
                userCallCodePath[0] = startCellName;//按照仓位下达任务
                userCallCodePath[1] = endCellName;//按照仓位下达任务
            }

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, null,
            startCellName, endCellName, null, type);
            var result = await _agvTaskRepository.InsertAsync(entity);

            await SetAsExecutingAsync(entity);

            return result;
        }



        [UnitOfWork]
        public async Task<AgvTask> CreateSkipCallTaskAsync(string startCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            string taskTyp = "";
            string[] userCallCodePath = null;
            taskTyp = _aGVOptions.SkipCallType;
            userCallCodePath = new string[3];
            userCallCodePath[0] = "1";
            userCallCodePath[1] = startCellName;//按照仓位下达任务
            userCallCodePath[2] = "";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, null,
            startCellName, null, null, type);
            var result = await _agvTaskRepository.InsertAsync(entity);

            await SetAsExecutingAsync(entity);

            return result;
        }

        [UnitOfWork]
        public async Task<AgvTask> CreateSkipMoveAsync(string startCellName, string endCellName, string podCode, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.SkipTaskType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName;//按照仓位下达任务
            userCallCodePath[1] = endCellName;//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath,
            startCellName, endCellName, type);
            var result = await _agvTaskRepository.InsertAsync(entity);

            await SetAsExecutingAsync(entity);

            return result;
        }


        [UnitOfWork]
        public async Task<AgvTask> CreateLiftStockOutByStockTaskAsync(string boxCode, string ctnrTyp, string startCellName, string endCellName, ManageType type)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.LiftTaskType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName + "${05}";//按照仓位下达任务
            userCallCodePath[1] = endCellName + "${05}";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, null, userCallCodePath, boxCode,
            startCellName, endCellName, ctnrTyp, type);
            var result = await _agvTaskRepository.InsertAsync(entity);

            await SetAsExecutingAsync(entity);

            return result;
        }


        [UnitOfWork]
        public async Task<AgvTask> CreateCTUStockOutByStockTaskAsync(string boxCode, string ctnrTyp, string startCellName, string endCellName, string podCode, ManageType type, string picklist, string unique)
        {
            var reqCode = Guid.NewGuid().ToString("N");
            var taskTyp = _aGVOptions.CTUTaskType;
            string[] userCallCodePath = new string[2];
            userCallCodePath[0] = startCellName + "${05}";//按照仓位下达任务
            userCallCodePath[1] = endCellName + "${05}";//按照仓位下达任务

            var entity = new AgvTask(reqCode, taskTyp, podCode, userCallCodePath, boxCode,
            startCellName, endCellName, ctnrTyp, type, picklist, unique);

            var result = await _agvTaskRepository.InsertAsync(entity, true);

            await SetAsExecutingAsync(entity);

            return result;
        }


        public async Task DeleteAsync(int agvTaskId)
        {
            var entity = await _agvTaskRepository.FindByIdAsync(agvTaskId);
            if (entity == null)
                throw new UserFriendlyException(message: "物料盒不存在");
            await _agvTaskRepository.DeleteAsync(entity);
        }
        public async Task<AgvTask> UpdateAsync(int id, string reqCode, string clientCode, string taskTyp,
            string wbCode, string podCode, string materialLot)
        {
            var entity = await _agvTaskRepository.FindByIdAsync(id);
            if (entity == null)
                throw new UserFriendlyException(message: "物料盒不存在");
            entity.Update(reqCode, clientCode, taskTyp, wbCode, podCode, materialLot);
            return await _agvTaskRepository.UpdateAsync(entity);
        }

        [UnitOfWork]
        public async Task<AgvTask> SetAsCompletedAsync(string reqcode)
        {
            try
            {
                var entity = await _agvTaskRepository.FindByReqCodeAsync(reqcode);
                if (entity == null)
                    throw new UserFriendlyException(message: "AGV任务不存在");
                // 完成回调重复到达时不再次解锁，避免释放后来任务对共享仓位的锁。
                if (entity.AgvTaskStatus == AgvTaskStatus.Complete)
                    return entity;
                //if (entity.AgvTaskStatus == AgvTaskStatus.Complete)
                //{
                //    _logger.Error(reqcode + "AgvTask任务重复完成");
                //    return entity;
                //}
                entity.SetAsCompleted();

                //设置结束库位状态
                string endCode = entity.EndPositionCode;
                Cell endCell = await _cellRepository.FindByCellCodeAsync(entity.EndPositionCode); ;

                _logger.Info($"设置目标库位{endCell.CellCode}为Enable");
                await SetSharedCellRunStatusAsync(endCell, CellRunStatus.Enable, CellStatus.Have);

                //设置开始库位状态
                string startCode = entity.StartPositionCode;
                Cell startCell = await _cellRepository.FindByCellCodeAsync(entity.StartPositionCode);

                _logger.Info($"设置开始库位{startCell.CellCode}为Enable");
                await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Enable, CellStatus.Nohave);

                /*Box box = null;
                if (!(entity.StockTyp == ManageType.SkipMove || entity.StockTyp == ManageType.SkipCall || entity.StockTyp == ManageType.SkipSend
                    ))
                {
                    box = await _boxRepository.FindByBoxCodeAsync(entity.BoxCode);
                    if (box == null)
                        throw new UserFriendlyException(message: "料箱不存在");

                    Warehouse warehouse = await _warehouseRepository.FindByIdAsync(endCell.WarehouseId).ConfigureAwait(false);
                    WarehouseArea warehouseArea = warehouse.GetAreaByAreaId((int)endCell.WarehouseAreaId);

                    box.BindCell(endCell, warehouse, warehouseArea);
                    await _boxRepository.UpdateAsync(box);
                }*/

                _logger.Info($"AGVTask:{reqcode} SetAsCompleted is end");
                return await _agvTaskRepository.UpdateAsync(entity, true);
            }
            catch (Exception e)
            {

                throw new UserFriendlyException(message: e.Message);
            }
        }


        public async Task<AgvTask> SetAsCancelAsync(string taskId, bool isActiveCancel = true)
        {
            try
            {
                var entity = await _agvTaskRepository.FindByReqCodeAsync(taskId);
                if (entity == null)
                    throw new UserFriendlyException(message: "AGV任务不存在");
                if (entity.AgvTaskStatus == AgvTaskStatus.Complete || entity.AgvTaskStatus == AgvTaskStatus.Cancel)
                    throw new UserFriendlyException(message: "AGV任务已完成或取消");
                entity.SetAsCancel();

                // 只有主动取消时才调用 RCS API 取消任务
                if (isActiveCancel)
                {
                    string req = Guid.NewGuid().ToString("N");
                    try
                    {
                        // 调用 RcsApiManager 取消任务
                        var cancelResult = await _rcsApiManager.CancelTaskAsync(req, entity.ReqCode);
                        if (cancelResult.Code != "0")
                        {
                            // RCS 取消失败不阻断本地取消，记录错误信息后继续执行本地取消业务
                            _logger.Error($"取消AGV任务失败: {cancelResult.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // RCS 接口调用异常（网络/超时等）同样不阻断本地取消
                        _logger.Error($"调用RCS取消任务接口异常: {entity.ReqCode}, 异常: {ex}");
                    }
                }


                //设置库位状态

                string endCode = entity.EndPositionCode;
                Cell endCell = null;
                //设置库位状态
                if (entity.EndPositionCode == "300015A1501013" || entity.EndPositionCode == "300016A1501013" ||
                    entity.EndPositionCode == "300017A1501013" || entity.EndPositionCode == "300018A1501013" || entity.EndPositionCode == "300019A1501013")
                {
                    endCell = await _cellRepository.FindByCellCode2Async(entity.EndPositionCode);
                }
                else
                {
                    endCell = await _cellRepository.FindByCellCodeAsync(entity.EndPositionCode);
                }
                _logger.Info($"设置目标库位{endCell.CellCode}为Enable");
                await SetSharedCellRunStatusAsync(endCell, CellRunStatus.Enable);
                //var endCell = await _cellRepository.FindByCellCodeAsync(entity.EndPositionCode);
                //endCell.SetEnable();
                //_logger.Info($"设置目标库位{endCell.CellCode}为Enable");
                //await _cellRepository.UpdateAsync(endCell, true);

                var startCell = await _cellRepository.FindByCellCodeAsync(entity.StartPositionCode);
                _logger.Info($"设置开始库位{startCell.CellCode}为Enable");
                await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Enable);


                if (entity.StockTyp == ManageType.LiftStockOut)
                {
                    Skip skip = await _skipRepository.FindByCellIdAsync(endCell.Id);
                    if (skip != null)
                    {
                        skip.SkipStatus = SkipStatus.NoHave;
                        skip.SkipRunStatus = SkipRunStatus.Enable;
                        skip.TargetLocation = null;
                        skip.TargetCellType = null;
                        await _skipRepository.UpdateAsync(skip);
                    }
                }

                return await _agvTaskRepository.UpdateAsync(entity, true);
            }
            catch (Exception e)
            {

                throw new UserFriendlyException(message: e.Message);
            }
        }

        [UnitOfWork]
        public async Task<bool> CreatePreAsync(string position, string nextTask, string agvTyp)
        {
            //设置AGV执行任务
            var response = await _rcsApiManager.CreateCTUPre(position, nextTask, agvTyp);
            if (response.Code != "0")
            {
                throw new UserFriendlyException(message: response.Message);
            }
            return true;
        }

        [UnitOfWork]
        public async Task<AgvTask> SetAsExecutingAsync(AgvTask entity)
        {
            try
            {
                entity.SetAsExecuting();

                if (entity.TaskTyp == _aGVOptions.SkipTaskType)
                {
                    string[] userCallCodePath = new string[2];
                    userCallCodePath[0] = entity.StartPositionCode;
                    userCallCodePath[1] = entity.EndPositionCode;
                    //设置AGV执行任务
                    var response = await _rcsApiManager.CreateTaskAsync(entity.ReqCode, entity.TaskTyp, userCallCodePath
                        , entity.ReqCode, entity.PodCode);
                    if (response.Code != "0")
                    {
                        throw new UserFriendlyException(message: response.Message);
                    }
                }
                else if (entity.TaskTyp == _aGVOptions.SkipSendType)
                {
                    string[] userCallCodePath = new string[3];
                    userCallCodePath[0] = entity.StartPositionCode;
                    userCallCodePath[1] = entity.EndPositionCode;
                    userCallCodePath[2] = "1";
                    //设置AGV执行任务
                    var response = await _rcsApiManager.CreateTaskAsync(entity.ReqCode, entity.TaskTyp, userCallCodePath
                        , entity.ReqCode, entity.PodCode);
                    if (response.Code != "0")
                    {
                        throw new UserFriendlyException(message: response.Message);
                    }
                }
                else if (entity.TaskTyp == _aGVOptions.SkipCallType)
                {
                    string[] userCallCodePath = new string[3];
                    userCallCodePath[0] = "1";
                    userCallCodePath[1] = entity.StartPositionCode;
                    userCallCodePath[2] = entity.EndPositionCode;
                    //设置AGV执行任务
                    var response = await _rcsApiManager.CreateTaskAsync(entity.ReqCode, entity.TaskTyp, userCallCodePath
                        , entity.ReqCode, entity.PodCode);
                    if (response.Code != "0")
                    {
                        throw new UserFriendlyException(message: response.Message);
                    }
                }
                else
                {
                    string[] userCallCodePath = new string[2];
                    userCallCodePath[0] = entity.StartPositionCode + "${05}";
                    userCallCodePath[1] = entity.EndPositionCode + "${05}";
                    // 无可用仓位时解绑目标，重发使用新 reqCode；taskCode、路径和本地任务记录保持不变。
                    var response = await RcsTaskDispatchRecovery.SendAsync(entity.EndPositionCode, entity.ReqCode,
                        reqCode => _rcsApiManager.CreateStockTaskAsync(reqCode, entity.TaskTyp, entity.CtnrTyp,
                            userCallCodePath, entity.ReqCode, entity.BoxCode, entity.PodCode),
                        target =>
                        {
                            // 同一任务的目标解绑重复提交时复用编号，不与原任务的下发请求混用。
                            var unbindReqCode = RcsTaskDispatchRecovery.GetUnbindRequestCode(entity.ReqCode, target);
                            _logger.LogWarning("目标仓位 {CellCode} 解绑，reqCode: {ReqCode}，原任务: {TaskCode}",
                                target, unbindReqCode, entity.ReqCode);
                            return _rcsApiManager.BindCtnrAndBinAsync(unbindReqCode, target, "1", string.Empty, "0");
                        },
                        message => _logger.LogWarning("{RecoveryMessage}，任务编号: {TaskCode}", message, entity.ReqCode));
                    if (response?.Code != "0")
                    {
                        throw new UserFriendlyException(message: response?.Message ?? "RCS任务下发返回空响应");
                    }
                }

                return await _agvTaskRepository.UpdateAsync(entity);
            }
            catch (Exception e)
            {
                //Log.Error($"AGVTask:{agvTaskId.ToString()} SetAsExecuting is fail ErrorMsg:{e.Message}。");
                throw new UserFriendlyException(message: e.Message);
            }
        }

        [UnitOfWork]
        public async Task<AgvTask> SetAsExecutingSkipCallAsync(AgvTask entity)
        {
            try
            {
                List<Cell> cells = await _cellRepository.FindByAreaTypeAvailableAsync(12, CellType.Skip, "1");

                entity.SetAsExecuting();

                string[] userCallCodePath = new string[3];
                userCallCodePath[0] = "1";
                userCallCodePath[1] = entity.StartPositionCode;
                userCallCodePath[2] = entity.EndPositionCode;
                //设置AGV执行任务
                var response = await _rcsApiManager.CreateTaskAsync(entity.ReqCode, entity.TaskTyp, userCallCodePath
                    , entity.ReqCode, entity.PodCode);
                if (response.Code != "0")
                {
                    throw new UserFriendlyException(message: response.Message);
                }

                return await _agvTaskRepository.UpdateAsync(entity);
            }
            catch (Exception e)
            {
                //Log.Error($"AGVTask:{agvTaskId.ToString()} SetAsExecuting is fail ErrorMsg:{e.Message}。");
                throw new UserFriendlyException(message: e.Message);
            }
        }
        /// <summary>
        /// 任务设置为执行，
        /// </summary>
        /// <param name="agvTaskId"></param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        [UnitOfWork]
        public async Task<AgvTask> SetAsTaskStart(string rqecode)
        {
            try
            {
                var entity = await _agvTaskRepository.FindByReqCodeAsync(rqecode);
                if (entity == null)
                    throw new UserFriendlyException(message: "AGV任务不存在");
                entity.SetAsTaskStart();
                return await _agvTaskRepository.UpdateAsync(entity);
            }
            catch (Exception e)
            {

                throw new UserFriendlyException(message: e.Message);
            }
        }
        [UnitOfWork]
        public async Task<AgvTask> SetAsCellOut(string reqcode)
        {
            try
            {
                var entity = await _agvTaskRepository.FindByReqCodeAsync(reqcode);
                if (entity == null)
                    throw new UserFriendlyException(message: "AGV任务不存在");
                entity.SetAsCellOut();



                if (entity.StockTyp == ManageType.SkipMove || entity.StockTyp == ManageType.SkipCall || entity.StockTyp == ManageType.SkipSend)
                {
                    var startCell = await _cellRepository.FindByCellCodeAsync(entity.StartPositionCode);
                    _logger.Info($"设置开始库位{startCell.CellCode}为Enable");
                    await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Enable, CellStatus.Nohave);
                }


                return await _agvTaskRepository.UpdateAsync(entity);
            }
            catch (Exception e)
            {

                throw new UserFriendlyException(message: e.Message);
            }
        }
        /// <summary>
        /// 任务设置为到达目标位置
        /// </summary>
        /// <param name="rqecode"></param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
       [UnitOfWork]
        public async Task<AgvTask> SetAsTaskArrive(string rqecode)
        {
            try
            {
                var entity = await _agvTaskRepository.FindByReqCodeAsync(rqecode);
                if (entity == null)
                    throw new UserFriendlyException(message: "AGV任务不存在");
                entity.SetAsTaskArrive();
                return await _agvTaskRepository.UpdateAsync(entity);
            }
            catch (Exception e)
            {

                throw new UserFriendlyException(message: e.Message);
            }
        }

        public async Task<AgvTask> FindByIdAsync(int taskId)
        {
            return await _agvTaskRepository.FindByIdAsync(taskId);
        }

        public async Task<AgvTask> FindByReqCodeAsync(string reqCode)
        {
            return await _agvTaskRepository.FindByReqCodeAsync(reqCode);
        }

        /// <summary>
        /// 是否存在重复任务
        /// </summary>
        /// <param name="boxCode"></param>
        /// <param name="taskTyp"></param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<bool> IsExistBoxTask(string boxCode)
        {
            if (boxCode != null)
            {
                var agvTask = await _agvTaskRepository.GetListAsync(f => f.BoxCode == boxCode
                & (f.AgvTaskStatus != AgvTaskStatus.Complete & f.AgvTaskStatus != AgvTaskStatus.Cancel));
                if (agvTask.Count > 0)
                { return true; }
                else
                { return false; }
            }
            else
            {
                throw new UserFriendlyException(message: "料箱编码为空");
            }
        }



        /// <summary>
        /// 是否存在重复任务
        /// </summary>
        /// <param name="boxCode"></param>
        /// <param name="taskTyp"></param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<bool> IsExistSkipTask(string skipCode)
        {
            if (skipCode != null)
            {
                var agvTask = await _agvTaskRepository.GetListAsync(f => f.PodCode == skipCode
                & (f.AgvTaskStatus != AgvTaskStatus.Complete & f.AgvTaskStatus != AgvTaskStatus.Cancel) && f.TaskTyp != "B10");
                if (agvTask.Count > 0)
                { return true; }
                else
                { return false; }
            }
            else
            {
                throw new UserFriendlyException(message: "料箱编码为空");
            }
        }

        public async Task<bool> BindCtnrAndBinAsync(string stgBinCode, string ctnrTyp, string ctnrCode, string indBind)
        {
            try
            {
                var response = await _rcsApiManager.BindCtnrAndBinAsync(null, stgBinCode, ctnrTyp, ctnrCode, indBind);
                if (response.Code == "1")
                {
                    throw new UserFriendlyException(message: response.Message);
                }
                else
                {
                    return true;
                    //response.
                }

            }
            catch (Exception e)
            {
                throw new UserFriendlyException(message: e.Message);
            }
        }
        public async Task<bool> BindPodAndBerthAsync(string stgBinCode, string ctnrCode, string indBind, string podDir)
        {
            try
            {
                var response = await _rcsApiManager.BindPodAndBerthAsync(null, stgBinCode, ctnrCode, indBind, podDir);
                if (response.Code == "1")
                {
                    throw new UserFriendlyException(message: response.Message);
                }
                else
                {
                    return true;
                }

            }
            catch (Exception e)
            {
                throw new UserFriendlyException(message: e.Message);
            }
        }

        public async Task<List<AgvTask>> GetPagingListAsync(
            string filter, DateTime startCreationTime, DateTime endCreationTime, string agvTaskStatus,
            int skipCount, int pageSize, string sorting)
        {
            int? status = null;
            if (!string.IsNullOrEmpty(agvTaskStatus))
            {
                if (int.TryParse(agvTaskStatus, out int statusInt))
                {
                    status = statusInt;
                }
            }

            return await _agvTaskRepository.GetPagingListAsync(
                filter, null, status, startCreationTime, endCreationTime,
                skipCount, pageSize, sorting);
        }

        public async Task<long> GetPagingCountAsync(
            string filter, DateTime startCreationTime, DateTime endCreationTime, string agvTaskStatus)
        {
            int? status = null;
            if (!string.IsNullOrEmpty(agvTaskStatus))
            {
                if (int.TryParse(agvTaskStatus, out int statusInt))
                {
                    status = statusInt;
                }
            }

            return await _agvTaskRepository.GetPagingCountAsync(
                filter, null, status, startCreationTime, endCreationTime);
        }

        /// <summary>
        /// 按机台和搬运方向查找区域 1 的空盒衬候选库位。对应货架尚未配置时，沿用旧规则。
        /// </summary>
        /// <param name="machineCellCode">机台交互点位的库位编码，用于查找所属机台。</param>
        /// <param name="toMachine">上机台时取有货库位，下机台时取空库位。</param>
        /// <returns>方向和有无货状态符合要求的可用库位。</returns>
        internal async Task<List<Cell>> FindEmptyBoxShelfCellsAsync(string machineCellCode, bool toMachine)
        {
            var machinePoints = await _machinePointRepository.GetListAsync(p => p.CellCode == machineCellCode);
            var machinePoint = machinePoints.FirstOrDefault();
            var machine = machinePoint == null ? null : await _machineRepository.GetAsync(machinePoint.MachineId);
            var machineName = machine?.Name;
            var requiredStatus = toMachine ? CellStatus.Have : CellStatus.Nohave;
            var areaCells = await _cellRepository.GetListAsync(cell => cell.WarehouseAreaId == 1);

            if (!string.IsNullOrWhiteSpace(machineName))
            {
                // 末尾 -1 为上料架，-2 为回收架。按完整名称匹配，避免相似机台名称串用货架。
                var shelfName = machineName + (toMachine ? "-1" : "-2");
                var configuredCells = areaCells.Where(cell => MatchesMachineShelf(cell, shelfName, exactLegacyName: true)).ToList();
                if (configuredCells.Count > 0)
                {
                    // 专用货架缺货、已满或被锁定时，只返回该架的可用库位。
                    return configuredCells.Where(cell =>
                        cell.RunStatus == CellRunStatus.Enable && cell.CellStatus == requiredStatus).ToList();
                }
            }

            if (toMachine)
            {
                return areaCells.Where(cell => cell.GetMachineShelfAssignment() == null &&
                    cell.RunStatus == CellRunStatus.Enable && cell.CellStatus == requiredStatus &&
                    string.IsNullOrEmpty(cell.ShelfName)).ToList();
            }

            if (string.IsNullOrWhiteSpace(machineName))
                return new List<Cell>();

            // 兼容旧回收架时排除新编号，避免把上料架或名称相似机台的回收架选为目标。
            return areaCells.Where(cell => cell.GetMachineShelfAssignment() == null &&
                cell.RunStatus == CellRunStatus.Enable && cell.CellStatus == requiredStatus &&
                MatchesMachineShelf(cell, machineName) &&
                !cell.ShelfName.EndsWith("-1") && !cell.ShelfName.EndsWith("-2")).ToList();
        }

        /// <summary>
        /// 按完整机台名称匹配 PDA 绑定，未设置绑定的库位沿用原货架名称匹配规则。
        /// </summary>
        internal static bool MatchesMachineShelf(Cell cell, string name, bool exactLegacyName = false)
        {
            var assignment = cell.GetMachineShelfAssignment();
            if (assignment != null)
                return assignment.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(name, StringComparer.OrdinalIgnoreCase);

            return exactLegacyName
                ? string.Equals(cell.ShelfName, name, StringComparison.OrdinalIgnoreCase)
                : cell.ShelfName?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// 创建空盒衬上机台任务
        /// </summary>
        /// <param name="machineCellCode">机台库位编码</param>
        /// <returns>AGV任务</returns>
        [UnitOfWork]
        public async Task<AgvTask> CreateEmptyBoxLiningTaskAsync(string machineCellCode)
        {
            try
            {
                _logger.Info($"开始创建空盒衬上机台任务，机台库位: {machineCellCode}");

                // 1. 先检查机台库位是否已经被锁定
                var endCell = await _cellRepository.FindByCellCodeAsync(machineCellCode);
                if (endCell == null)
                {
                    throw new UserFriendlyException($"机台库位{machineCellCode}不存在");
                }
                
                // 检查机台库位是否已经被锁定
                if (endCell.RunStatus == CellRunStatus.Selected)
                {
                    throw new UserFriendlyException($"机台库位{machineCellCode}已经被锁定，不能重复下发任务");
                }

                // 2. 查找上空盒衬货架的有货库位
                var emptyBoxCells = await FindEmptyBoxShelfCellsAsync(machineCellCode, toMachine: true);
                
                if (emptyBoxCells == null || emptyBoxCells.Count == 0)
                {
                    throw new UserFriendlyException("没有找到空衬托上架区域的有货库位");
                }

                // 4. 选择第一个可用的空衬托库位
                var startCell = await FindUnlockedSharedCellAsync(emptyBoxCells);
                if (startCell == null)
                {
                    throw new UserFriendlyException("没有可用的空衬托库位");
                }

                // 5. 锁定开始库位和结束库位
                await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Selected);
                _logger.Info($"锁定开始库位: {startCell.CellCode}");

                await SetSharedCellRunStatusAsync(endCell, CellRunStatus.Selected);
                _logger.Info($"锁定结束库位: {endCell.CellCode}");

                // 6. 直接使用库位编码作为任务参数，不需要容器
                //var boxCode = startCell.CellCode; // 使用库位编码作为容器编码
                var boxTypeName = "1"; // 固定容器类型

                // 7. 创建AGV任务
                var reqCode = Guid.NewGuid().ToString("N");
                var taskTyp = _aGVOptions.CTUTaskType;
                string[] userCallCodePath = new string[2];
                userCallCodePath[0] = startCell.CellCode + "${05}"; // 按照仓位下达任务
                userCallCodePath[1] = endCell.CellCode + "${05}"; // 按照仓位下达任务

                var entity = new AgvTask(reqCode, taskTyp, null, userCallCodePath, null,
                    startCell.CellCode, endCell.CellCode, boxTypeName, ManageType.EmptyBoxToMachine);
                var result = await _agvTaskRepository.InsertAsync(entity);

                // 8. 设置任务为执行中
                await SetAsExecutingAsync(entity);

                _logger.Info($"空盒衬上机台任务创建成功，任务ID: {result.Id}, 从{startCell.CellCode}到{endCell.CellCode}");

                return result;
            }
            catch (Exception e)
            {
                _logger.Error($"创建空盒衬上机台任务失败: {e.Message}");
                throw new UserFriendlyException(message: e.Message);
            }
        }

        /// <summary>
        /// 创建成品下机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <param name="machineName">机台名称</param>
        /// <returns>AGV任务</returns>
        [UnitOfWork]
        public async Task<AgvTask> CreateFinishedFromMachineTaskAsync(string cellCode, string machineName)
        {
            try
            {
                _logger.Info($"开始创建成品下机台任务，开始库位: {cellCode}，机台名称: {machineName}");

                // 1. 查找开始库位（机台库位）
                var startCell = await _cellRepository.FindByCellCodeAsync(cellCode);
                if (startCell == null)
                {
                    throw new UserFriendlyException($"开始库位{cellCode}不存在");
                }
                
                // 检查开始库位是否已经被锁定
                if (startCell.RunStatus == CellRunStatus.Selected)
                {
                    throw new UserFriendlyException($"开始库位{cellCode}已经被锁定，不能重复下发任务");
                }

                // 2. 查找机台信息
                var machine = await _machineRepository.FirstOrDefaultAsync(m => m.Name == machineName);
                if (machine == null)
                {
                    throw new UserFriendlyException($"机台{machineName}不存在");
                }

                // 3. 查找对应机台的成品区货架库位
                var finishedProductCells = await _cellRepository.GetListAsync(t => 
                    t.WarehouseAreaId == 2 && 
                    t.RunStatus == CellRunStatus.Enable && 
                    t.CellStatus == CellStatus.Nohave);
                finishedProductCells = finishedProductCells.Where(cell => MatchesMachineShelf(cell, machineName)).ToList();
                    
                // 4. 按列号升序选位，同列内由下往上放置
                var endCell = await FindUnlockedFinishedShelfCellAsync(finishedProductCells);
                if (endCell == null)
                {
                    throw new UserFriendlyException("无法选择成品区库位");
                }

                // 5. 锁定开始库位和结束库位
                await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Selected);
                _logger.Info($"锁定开始库位: {startCell.CellCode}");

                await SetSharedCellRunStatusAsync(endCell, CellRunStatus.Selected);
                _logger.Info($"锁定结束库位: {endCell.CellCode}");

                // 6. 直接使用库位编码作为任务参数
                //var boxCode = startCell.CellCode; // 使用开始库位编码作为容器编码
                var boxTypeName = "1"; // 固定容器类型

                // 6. 创建AGV任务
                var reqCode = Guid.NewGuid().ToString("N");
                var taskTyp = _aGVOptions.CTUTaskType;
                string[] userCallCodePath = new string[2];
                userCallCodePath[0] = startCell.CellCode + "${05}"; // 按照仓位下达任务
                userCallCodePath[1] = endCell.CellCode + "${05}"; // 按照仓位下达任务

                var entity = new AgvTask(reqCode, taskTyp, null, userCallCodePath, null,
                    startCell.CellCode, endCell.CellCode, boxTypeName, ManageType.FinishedFromMachine);
                var result = await _agvTaskRepository.InsertAsync(entity);

                // 7. 设置任务为执行中
                await SetAsExecutingAsync(entity);

                _logger.Info($"成品下机台任务创建成功，任务ID: {result.Id}, 从{startCell.CellCode}到{endCell.CellCode}，机台: {machineName}");

                return result;
            }
            catch (Exception e)
            {
                _logger.Error($"创建成品下机台任务失败: {e.Message}");
                throw new UserFriendlyException(message: e.Message);
            }
        }

        /// <summary>
        /// 创建空盒衬下机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <returns>AGV任务</returns>
        [UnitOfWork]
        public async Task<AgvTask> CreateEmptyBoxFromMachineTaskAsync(string cellCode)
        {
            try
            {
                _logger.Info($"开始创建空盒衬下机台任务，开始库位: {cellCode}");

                // 1. 查找开始库位（机台库位）
                var startCell = await _cellRepository.FindByCellCodeAsync(cellCode);
                if (startCell == null)
                {
                    throw new UserFriendlyException($"开始库位{cellCode}不存在");
                }
                
                // 检查开始库位是否已经被锁定
                if (startCell.RunStatus == CellRunStatus.Selected)
                {
                    throw new UserFriendlyException($"开始库位{cellCode}已经被锁定，不能重复下发任务");
                }

                // 2. 查找下空盒衬货架的空库位
                var emptyBoxCells = await FindEmptyBoxShelfCellsAsync(cellCode, toMachine: false);
                
                if (emptyBoxCells == null || emptyBoxCells.Count == 0)
                {
                    throw new UserFriendlyException("没有找到空盒衬区域的可用库位");
                }

                // 3. 优先选择最低层的可用空盒衬库位
                var endCell = await FindLowestUnlockedShelfCellAsync(emptyBoxCells);
                if (endCell == null)
                {
                    throw new UserFriendlyException("无法选择空盒衬库位");
                }

                // 4. 锁定开始库位和结束库位
                await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Selected);
                _logger.Info($"锁定开始库位: {startCell.CellCode}");

                await SetSharedCellRunStatusAsync(endCell, CellRunStatus.Selected);
                _logger.Info($"锁定结束库位: {endCell.CellCode}");

                // 5. 直接使用库位编码作为任务参数
                //var boxCode = startCell.CellCode; // 使用开始库位编码作为容器编码
                var boxTypeName = "1"; // 固定容器类型

                // 6. 创建AGV任务
                var reqCode = Guid.NewGuid().ToString("N");
                var taskTyp = _aGVOptions.CTUTaskType;
                string[] userCallCodePath = new string[2];
                userCallCodePath[0] = startCell.CellCode + "${05}"; // 按照仓位下达任务
                userCallCodePath[1] = endCell.CellCode + "${05}"; // 按照仓位下达任务

                var entity = new AgvTask(reqCode, taskTyp, null, userCallCodePath, null,
                    startCell.CellCode, endCell.CellCode, boxTypeName, ManageType.EmptyBoxFromMachine);
                var result = await _agvTaskRepository.InsertAsync(entity);

                // 7. 设置任务为执行中
                await SetAsExecutingAsync(entity);

                _logger.Info($"空盒衬下机台任务创建成功，任务ID: {result.Id}, 从{startCell.CellCode}到{endCell.CellCode}");

                return result;
            }
            catch (Exception e)
            {
                _logger.Error($"创建空盒衬下机台任务失败: {e.Message}");
                throw new UserFriendlyException(message: e.Message);
            }
        }

        /// <summary>
        /// 创建半成品上料任务（半成品加工上料）
        /// </summary>
        /// <param name="cellCode">结束库位编码（机台库位）</param>
        /// <param name="machineName">机台名称</param>
        /// <returns>AGV任务</returns>
        [UnitOfWork]
        public async Task<AgvTask> CreateSemiFinishedFromMachineTaskAsync(string cellCode, string machineName)
        {
            try
            {
                _logger.Info($"开始创建半成品上料任务，结束库位: {cellCode}，机台名称: {machineName}");

                // 1. 查找结束库位（机台库位）
                var endCell = await _cellRepository.FindByCellCodeAsync(cellCode);
                if (endCell == null)
                {
                    throw new UserFriendlyException($"结束库位{cellCode}不存在");
                }
                
                // 检查结束库位是否已经被锁定
                if (endCell.RunStatus == CellRunStatus.Selected)
                {
                    throw new UserFriendlyException($"结束库位{cellCode}已经被锁定，不能重复下发任务");
                }

                // 2. 查找机台信息
                var machine = await _machineRepository.FirstOrDefaultAsync(m => m.Name == machineName);
                if (machine == null)
                {
                    throw new UserFriendlyException($"机台{machineName}不存在");
                }

                // 3. 查找对应机台的半成品区域有料库位
                var semiFinishedCells = await _cellRepository.GetListAsync(t => 
                    t.WarehouseAreaId == 3 && 
                    t.RunStatus == CellRunStatus.Enable && 
                    t.CellStatus == CellStatus.Have);
                semiFinishedCells = semiFinishedCells.Where(cell => MatchesMachineShelf(cell, machineName)).ToList();

                if (semiFinishedCells == null || semiFinishedCells.Count == 0)
                {
                    throw new UserFriendlyException($"没有找到机台{machineName}对应的半成品区域有料库位");
                }

                // 4. 选择第一个可用的半成品有料库位
                var startCell = await FindUnlockedSharedCellAsync(semiFinishedCells);
                if (startCell == null)
                {
                    throw new UserFriendlyException("无法选择半成品有料库位");
                }

                // 5. 锁定开始库位和结束库位
                await SetSharedCellRunStatusAsync(startCell, CellRunStatus.Selected);
                _logger.Info($"锁定开始库位: {startCell.CellCode}");

                await SetSharedCellRunStatusAsync(endCell, CellRunStatus.Selected);
                _logger.Info($"锁定结束库位: {endCell.CellCode}");

                // 6. 直接使用库位编码作为任务参数
                //var boxCode = startCell.CellCode; // 使用开始库位编码作为容器编码
                var boxTypeName = "1"; // 固定容器类型

                // 5. 创建AGV任务
                var reqCode = Guid.NewGuid().ToString("N");
                var taskTyp = _aGVOptions.CTUTaskType;
                string[] userCallCodePath = new string[2];
                userCallCodePath[0] = startCell.CellCode + "${05}"; // 按照仓位下达任务
                userCallCodePath[1] = endCell.CellCode + "${05}"; // 按照仓位下达任务

                var entity = new AgvTask(reqCode, taskTyp, null, userCallCodePath, null,
                    startCell.CellCode, endCell.CellCode, boxTypeName, ManageType.SemiFinishedFromMachine);
                var result = await _agvTaskRepository.InsertAsync(entity);

                // 6. 设置任务为执行中
                await SetAsExecutingAsync(entity);

                _logger.Info($"半成品上料任务创建成功，任务ID: {result.Id}, 从{startCell.CellCode}到{endCell.CellCode}，机台: {machineName}");

                return result;
            }
            catch (Exception e)
            {
                _logger.Error($"创建半成品上料任务失败: {e.Message}");
                throw new UserFriendlyException(message: e.Message);
            }
        }

        /// <summary>
        /// 继续执行AGV任务
        /// </summary>
        /// <param name="reqCode">请求编号</param>
        /// <param name="taskCode">任务编号</param>
        /// <returns>结果</returns>
        public async Task ContinueTaskAsync(string reqCode, string taskCode)
        {
            try
            {
                _logger.Info($"继续执行AGV任务，请求编号: {reqCode}，任务编号: {taskCode}");
                
                // 调用RCS API继续执行任务
                var response = await _rcsApiManager.ContinueTaskAsync(reqCode, taskCode);
                if (response.Code != "0")
                {
                    throw new UserFriendlyException(message: response.Message);
                }
                
                _logger.Info($"继续执行AGV任务成功，请求编号: {reqCode}，任务编号: {taskCode}");
            }
            catch (Exception e)
            {
                _logger.Error($"继续执行AGV任务失败: {e.Message}");
                throw new UserFriendlyException(message: e.Message);
            }
        }

        /// <summary>
        /// 料箱取放
        /// </summary>
        /// <param name="taskCode">任务编号</param>
        /// <param name="type">类型：1-取申请通过，2-放申请通过</param>
        /// <returns>结果</returns>
        public async Task BoxApplyPassAsync(string taskCode, string type)
        {
            try
            {
                // 随机生成请求编号
                string reqCode = Guid.NewGuid().ToString("N");
                
                _logger.Info($"料箱取放，请求编号: {reqCode}，任务编号: {taskCode}，类型: {type}");
                
                // 调用RCS API执行料箱取放
                var response = await _rcsApiManager.BoxApplyPassAsync(reqCode, taskCode, type);
                if (response.Code != "0")
                {
                    throw new UserFriendlyException(message: response.Message);
                }
                
                _logger.Info($"料箱取放成功，请求编号: {reqCode}，任务编号: {taskCode}，类型: {type}");
            }
            catch (Exception e)
            {
                _logger.Error($"料箱取放失败: {e.Message}");
                throw new UserFriendlyException(message: e.Message);
            }
        }
    }
}
