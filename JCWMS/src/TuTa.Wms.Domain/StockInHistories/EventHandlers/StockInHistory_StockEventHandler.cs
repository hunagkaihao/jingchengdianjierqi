using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TuTa.Wms.StockInHistories.Aggregates;
using TuTa.Wms.StockInHistories.ValueObjects;
using TuTa.Wms.Stocks.Events;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;
using Wms.LogTool;

namespace TuTa.Wms.StockInHistories.EventHandlers
{
    public class StockInHistory_StockEventHandler
         : ILocalEventHandler<StockBindBoxAndCellEvent>, //物料人工入库
           ITransientDependency
    {
        private readonly IStockInHistoryRepository _stockHistoryRepository;
        private readonly UnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<StockInHistory_StockEventHandler> _logger;
        private readonly LocalEventBus _localEventBus;

        private static readonly object _locker = new object();

        public StockInHistory_StockEventHandler(
            IStockInHistoryRepository stockHistoryRepository,
            UnitOfWorkManager unitOfWorkManager,
            ILogger<StockInHistory_StockEventHandler> logger,
            LocalEventBus localEventBus)
        {
            _stockHistoryRepository = stockHistoryRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
            _localEventBus = localEventBus;
        }

        public async Task HandleEventAsync(StockBindBoxAndCellEvent eventData)
        {
            //TODO: your code that does somthing on the event
            using (IUnitOfWork uow = _unitOfWorkManager.Begin())
            {
                try
                {
                    if (eventData == null)
                        throw new ArgumentNullException(nameof(eventData));

                    StockInHistory stockHistory = new StockInHistory(
                        eventData.StockBarcode,
                        new MaterialInfoOfStockInHistory(eventData.MaterialCode, eventData.MaterialName, eventData.Specs, eventData.Unit),
                        new CheckInfoOfStockInHistory(eventData.CheckOrderCode, eventData.CheckNo, eventData.CheckDate, eventData.CheckResult),
                        new SupplierInfoOfStockInHistory(eventData.SupplierCode, eventData.SupplierName),
                        new StockPlaceOfStockInHistory(eventData.HouseCode, eventData.HouseName, eventData.AreaCode, eventData.AreaName,
                        eventData.CellCode, eventData.CellName, eventData.BoxCode, eventData.BoxName),
                        eventData.StockInType, eventData.StockCount, eventData.StockInDate, eventData.Operator);

                    await _stockHistoryRepository.InsertAsync(stockHistory).ConfigureAwait(false);

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
