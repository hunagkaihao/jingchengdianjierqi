using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks.Aggregaes;
using Volo.Abp.Domain.Repositories;

namespace TuTa.Wms.AgvTasks
{
    public interface IAgvTaskRepository : IRepository<AgvTask, int>
    {
        Task<AgvTask> FindByIdAsync(
            int id, bool isTrack = true, CancellationToken cancellationToken = default);


        Task<AgvTask> FindByReqCodeAsync(
            string reqcode, bool isTrack = true, CancellationToken cancellationToken = default);

        Task<List<AgvTask>> GetPagingListAsync(
            string filter, string boxCode, int? status, DateTime startCreationTime, DateTime endCreationTime,
            int skipCount, int pageSize, string sorting, bool isTrack = true,
            CancellationToken cancellationToken = default);

        Task<long> GetPagingCountAsync(
            string filter, string boxCode, int? status, DateTime startCreationTime, DateTime endCreationTime,
            CancellationToken cancellationToken = default);
    }
}
