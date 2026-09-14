using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TuTa.Wms.StockInHistories.Aggregates;
using TuTa.Wms.StockOutHistories.ValueObjects;
using TuTa.Wms.Stocks.Events;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;
using Wms.LogTool;

namespace TuTa.Wms.StockOutHistories.EventHandlers
{
    public class StockOutHistory_StockEventHandler
         : ILocalEventHandler<StockPickOutEvent>, //正常领用
           ILocalEventHandler<StockRecheckOutEvent>, //复检领用
           ITransientDependency
    {
        private readonly IStockOutHistoryRepository _stockOutHistoryRepository;
        private readonly UnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<StockOutHistory_StockEventHandler> _logger;
        private readonly LocalEventBus _localEventBus;

        private static readonly object _locker = new object();

        public StockOutHistory_StockEventHandler(
            IStockOutHistoryRepository stockOutHistoryRepository,
            UnitOfWorkManager unitOfWorkManager,
            ILogger<StockOutHistory_StockEventHandler> logger,
            LocalEventBus localEventBus)
        {
            _stockOutHistoryRepository = stockOutHistoryRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
            _localEventBus = localEventBus;
        }

        public async Task HandleEventAsync(StockPickOutEvent eventData)
        {
            //TODO: your code that does somthing on the event
            using (IUnitOfWork uow = _unitOfWorkManager.Begin())
            {
                try
                {
                    if (eventData == null)
                        throw new ArgumentNullException(nameof(eventData));

                    StockOutHistory stockHistory = new StockOutHistory(
                        eventData.Barcode,
                        new MaterialInfoOfStockOutHistory(
                            eventData.Material.MaterialCode, 
                            eventData.Material.MaterialName, 
                            eventData.Material.Specs, 
                            eventData.Material.Unit),
                        new CheckInfoOfStockOutHistory(
                            eventData.CheckData.CheckOrderCode, 
                            eventData.CheckData.CheckNo,
                            eventData.CheckData.CheckDate,
                            eventData.CheckData.CheckResultInChs(),
                            eventData.CheckData.CheckNoBeforeReCheck,
                            eventData.CheckData.CheckType),
                        new SupplierInfoOfStockOutHistory(
                            eventData.Supplier.SupplierCode, 
                            eventData.Supplier.SupplierName),
                        new PickerOfStockOutHistory(
                            eventData.DeptCode, 
                            eventData.DeptName, 
                            eventData.GysCode, 
                            eventData.GysName),
                        new GoodsInfoOfStockOutHistory(
                            eventData.GoodsCode,
                            eventData.GoodsName,
                            eventData.GoodsSpecs),
                        new StockPlaceOfStockOutHistory(eventData.Warehouse.HouseCode, eventData.Warehouse.HouseName, eventData.Warehouse.AreaCode, eventData.Warehouse.AreaName,
                        eventData.CellData.CellCode, eventData.CellData.CellName, eventData.BoxData.BoxCode, eventData.BoxData.BoxName),
                        eventData.StockOutTypeInChs, eventData.PickBatch, eventData.UniqueCode,
                        eventData.StockOutCount, eventData.StockOutTime, eventData.OperatorName);

                    await _stockOutHistoryRepository.InsertAsync(stockHistory).ConfigureAwait(false);

                    await uow.SaveChangesAsync().ConfigureAwait(false);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    //await uow.RollbackAsync().ConfigureAwait(false);
                    throw new UserFriendlyException(ex.Message);
                }
            }
        }

        public async Task HandleEventAsync(StockRecheckOutEvent eventData)
        {
            //TODO: your code that does somthing on the event
            using (IUnitOfWork uow = _unitOfWorkManager.Begin())
            {
                try
                {
                    if (eventData == null)
                        throw new ArgumentNullException(nameof(eventData));

                    StockOutHistory stockHistory = new StockOutHistory(
                        eventData.Barcode,
                        new MaterialInfoOfStockOutHistory(
                            eventData.Material.MaterialCode,
                            eventData.Material.MaterialName,
                            eventData.Material.Specs,
                            eventData.Material.Unit),
                        new CheckInfoOfStockOutHistory(
                            eventData.CheckData.CheckOrderCode,
                            eventData.CheckData.CheckNo,
                            eventData.CheckData.CheckDate,
                            eventData.CheckData.CheckResultInChs(),
                            eventData.CheckData.CheckNoBeforeReCheck,
                            eventData.CheckData.CheckType),
                        new SupplierInfoOfStockOutHistory(
                            eventData.Supplier.SupplierCode,
                            eventData.Supplier.SupplierName),
                        new PickerOfStockOutHistory(
                            eventData.DeptCode,
                            eventData.DeptName,
                            eventData.GysCode,
                            eventData.GysName),
                        new GoodsInfoOfStockOutHistory(
                            eventData.GoodsCode,
                            eventData.GoodsName,
                            eventData.GoodsSpecs),
                        new StockPlaceOfStockOutHistory(eventData.Warehouse.HouseCode, eventData.Warehouse.HouseName, eventData.Warehouse.AreaCode, eventData.Warehouse.AreaName,
                        eventData.CellData.CellCode, eventData.CellData.CellName, eventData.BoxData.BoxCode, eventData.BoxData.BoxName),
                        eventData.StockOutTypeInChs, eventData.PickBatch, eventData.UniqueCode,
                        eventData.StockOutCount, eventData.StockOutTime, eventData.OperatorName);

                    await _stockOutHistoryRepository.InsertAsync(stockHistory).ConfigureAwait(false);

                    await uow.SaveChangesAsync().ConfigureAwait(false);
                    await uow.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    //await uow.RollbackAsync().ConfigureAwait(false);
                    throw new UserFriendlyException(ex.Message);
                }
            }
        }
    }
}
