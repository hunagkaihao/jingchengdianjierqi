using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TuTa.Wms.Domain;
using TuTa.Wms.EntityFrameworkCore;
using TuTa.Wms.StockInHistories.Aggregates;
using TuTa.Wms.StockOutHistories;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Wms.EntityFrameworkCore;

namespace TuTa.Wms.Repositories.StockInReports
{
    public class EfCoreStockOutHistoryRepository : EfCoreRepository<WmsDbContext, StockOutHistory, int>, IStockOutHistoryRepository
    {
        public EfCoreStockOutHistoryRepository(IDbContextProvider<WmsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<QueryDataInPage<StockOutHistory>> GetPagedStockOutHistoriesAsync(
            string barcode, 
            string materialCode, string materialNameTip, string materialSpecsTip, 
            string stockOutType, 
            DateTime? outDateStart, DateTime? outDateEnd,
            string checkNoTip, string pickBatchTip,
            bool isTrack = true, 
            int skipCount = 0, 
            int maxResultCount = 10, 
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync().ConfigureAwait(false);
            IQueryable<StockOutHistory> querable = dbSet
                .TrackIf(isTrack)
                .Where(o =>
                (barcode == null ? true : o.Barcode == barcode) &&
                (materialCode == null ? true : o.Material.Code.Contains(materialCode)) &&
                (materialNameTip == null ? true : o.Material.Name.Contains(materialNameTip)) &&
                (materialSpecsTip == null ? true : o.Material.Specs.Contains(materialSpecsTip)) &&
                (stockOutType == null ? true : o.StockOutType == stockOutType) &&
                ((outDateStart == null || outDateEnd == null) ? true : o.OutTime >= outDateStart && o.OutTime <= outDateEnd) &&
                (checkNoTip == null ? true : o.CheckData.CheckNo.Contains(checkNoTip)) &&
                (pickBatchTip == null ? true : o.PickBatch.Contains(pickBatchTip)));

            return new QueryDataInPage<StockOutHistory>()
            {
                TotalCount = await querable.CountAsync(),
                Items = await querable
                    .OrderByDescending(o => o.CreationTime)
                    .PageBy(skipCount, maxResultCount)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false)
            };
        }
    }
}
