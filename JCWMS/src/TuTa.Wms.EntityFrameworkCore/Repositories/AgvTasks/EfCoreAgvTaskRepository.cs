using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TuTa.Wms.AgvTasks;
using TuTa.Wms.AgvTasks.Aggregaes;
using TuTa.Wms.EntityFrameworkCore;

using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

using Wms.EntityFrameworkCore;

namespace TuTa.Wms.Repositories.AgvTasks
{
    public class EfCoreAgvTaskRepository : EfCoreRepository<WmsDbContext, AgvTask, int>, IAgvTaskRepository
    {
        public EfCoreAgvTaskRepository(IDbContextProvider<WmsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<AgvTask> FindByIdAsync(
            int id,
            bool isTrack = true,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync().ConfigureAwait(false);
            return await dbSet
                .TrackIf(isTrack)
                .FirstOrDefaultAsync(o => o.Id == id)
                .ConfigureAwait(false);
        }

        public async Task<AgvTask> FindByReqCodeAsync(
            string reqcode,
            bool isTrack = true,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync().ConfigureAwait(false);
            return await dbSet
                .TrackIf(isTrack)
                .FirstOrDefaultAsync(o => o.ReqCode == reqcode)
                .ConfigureAwait(false);
        }

        public async Task<List<AgvTask>> GetPagingListAsync(
            string filter, string boxCode, int? status, DateTime startCreationTime, DateTime endCreationTime,
            int skipCount, int pageSize, string sorting, bool isTrack = true,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync().ConfigureAwait(false);
            var query = dbSet.TrackIf(isTrack);

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(o => o.ReqCode.Contains(filter) || o.BoxCode.Contains(filter));
            }

            if (!string.IsNullOrEmpty(boxCode))
            {
                query = query.Where(o => o.BoxCode == boxCode);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.AgvTaskStatus == (AgvTaskStatus)status.Value);
            }

            query = query.Where(o => o.CreationTime >= startCreationTime);
            query = query.Where(o => o.CreationTime <= endCreationTime);

            if (!string.IsNullOrEmpty(sorting))
            {
                // 简单处理排序，实际项目中可能需要更复杂的排序逻辑
                if (sorting.StartsWith("CreationTime desc"))
                {
                    query = query.OrderByDescending(o => o.CreationTime);
                }
                else if (sorting.StartsWith("CreationTime asc"))
                {
                    query = query.OrderBy(o => o.CreationTime);
                }
                else
                {
                    query = query.OrderByDescending(o => o.CreationTime);
                }
            }
            else
            {
                query = query.OrderByDescending(o => o.CreationTime);
            }

            return await query
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<long> GetPagingCountAsync(
            string filter, string boxCode, int? status, DateTime startCreationTime, DateTime endCreationTime,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync().ConfigureAwait(false);
            var query = dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(o => o.ReqCode.Contains(filter) || o.BoxCode.Contains(filter));
            }

            if (!string.IsNullOrEmpty(boxCode))
            {
                query = query.Where(o => o.BoxCode == boxCode);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.AgvTaskStatus == (AgvTaskStatus)status.Value);
            }

            query = query.Where(o => o.CreationTime >= startCreationTime);
            query = query.Where(o => o.CreationTime <= endCreationTime);

            return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
