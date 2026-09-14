using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TuTa.Wms.StockInHistories.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Wms.LogTool;

namespace TuTa.Wms.StockInHistories
{
    public class StockInHistoryService : WmsAppService, IStockInHistoryService
    {
        private readonly IStockInHistoryRepository _stockInHistoryRepository;
        private readonly ILogger<StockInHistoryService> _logger;

        public StockInHistoryService(
            IStockInHistoryRepository stockHistoryRepository,
            ILogger<StockInHistoryService> logger)
        {
            _stockInHistoryRepository = stockHistoryRepository;
            _logger = logger;
        }

        public async Task<PagedResultDto<StockInHistoryDto>> GetPagedStockInHistoriesAsync(PagedStockInHistoryQueryDto para)
        {
            try
            {
                var stockHistories = await _stockInHistoryRepository.GetPagedStockInHistoriesAsync(
                    para.Barcode,
                    para.MaterialCode, para.MaterialNameTip, para.MaterialSpecsTip,
                    para.StockInType,
                    para.StockInTimeStart, para.StockInTimeEnd, para.CheckNoTip,
                    false, para.SkipCount, para.MaxResultCount);

                if (stockHistories == null || stockHistories.TotalCount == 0 || stockHistories.Items == null) 
                    return new PagedResultDto<StockInHistoryDto>() { TotalCount = 0, Items = new List<StockInHistoryDto>() };

                PagedResultDto<StockInHistoryDto> result = new PagedResultDto<StockInHistoryDto>() { TotalCount = stockHistories.TotalCount };

                List<StockInHistoryDto> stockInHistoryDtos = new List<StockInHistoryDto>();
                foreach( var stockHistory in stockHistories.Items )
                {
                    StockInHistoryDto historyDto = new StockInHistoryDto()
                    {
                        Id = stockHistory.Id,
                        Barcode = stockHistory.Barcode,
                        BoxCode = stockHistory.StockPlace.BoxCode,
                        BoxName = stockHistory.StockPlace.BoxName,
                        CellCode = stockHistory.StockPlace.CellCode,
                        CellName = stockHistory.StockPlace.CellName,
                        AreaCode = stockHistory.StockPlace.AreaCode,
                        AreaName = stockHistory.StockPlace.AreaName,
                        HouseCode = stockHistory.StockPlace.HouseCode,
                        HouseName = stockHistory.StockPlace.HouseName,
                        StockInCount = stockHistory.InCount,
                        StockInTime = stockHistory.InTime,
                        MaterialCode = stockHistory.Material.Code,
                        MaterialName = stockHistory.Material.Name,
                        Specs = stockHistory.Material.Specs,
                        Unit = stockHistory.Material.Unit,
                        CheckOrderCode = stockHistory.CheckData.CheckOrderCode,
                        CheckDate = stockHistory.CheckData.CheckDate,
                        CheckNo = stockHistory.CheckData.CheckNo,
                        CheckResult = stockHistory.CheckData.CheckResult,
                        SupplierCode = stockHistory.Supplier.Code,
                        SupplierName = stockHistory.Supplier.Name,
                        StockInType = stockHistory.StockInType,
                        Operator = stockHistory.OperatorName
                    };
                    stockInHistoryDtos.Add(historyDto);
                }

                result.Items = stockInHistoryDtos;

                return result;

            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new UserFriendlyException(ex.Message);
            }
        }
    }
}
