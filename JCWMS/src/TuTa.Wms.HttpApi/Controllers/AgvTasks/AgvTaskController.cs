using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TuTa.Wms.AgvTasks;
using TuTa.Wms.AgvTasks.Dtos;
using TuTa.Wms.BarcodeLists;
using TuTa.Wms.BarcodeLists.Dtos;
using Volo.Abp.Application.Dtos;

namespace TuTa.Wms.Controllers.AgvTasks
{
    [Route("wms/agvtask")]
    [ApiController]
    public class AgvTaskController : WmsController
    {
        private IAgvTaskService _agvTaskService;

        public AgvTaskController(IAgvTaskService agvTaskService)
        {
            _agvTaskService = agvTaskService;
        }

        [HttpPost("callback")]
        [SwaggerOperation(summary: "CTU任务回调", Tags = new[] { "AgvTask" })]
        public async Task<ResultAgvTaskDto> CtuCallbackAsync(AgvCallBackRequest input)
        {
            return await _agvTaskService.CtuCallbackAsync(input);
        }

        [HttpPost("page")]
        [SwaggerOperation(summary: "获取AGV任务清单", Tags = new[] { "AgvTask" })]
        public async Task<PagedResultDto<AgvTaskDto>> GetPagingListAsync(PagingAgvTaskListInput input)
        {
            return await _agvTaskService.GetPagingListAsync(input);
        }

        [HttpPost("setAsExecuting")]
        [SwaggerOperation(summary: "将任务设置为执行中", Tags = new[] { "AgvTask" })]
        public async Task SetAsExecutingAsync(int taskId)
        {
            await _agvTaskService.SetAsExecutingAsync(taskId);
        }

        [HttpPost("setAsCompleted")]
        [SwaggerOperation(summary: "将任务设置为完成", Tags = new[] { "AgvTask" })]
        public async Task SetAsCompletedAsync(int taskId)
        {
            await _agvTaskService.SetAsCompletedAsync(taskId);
        }

        [HttpPost("setAsCancel")]
        [SwaggerOperation(summary: "将任务设置为取消", Tags = new[] { "AgvTask" })]
        public async Task SetAsCancelAsync(int taskId, bool isSync)
        {
            await _agvTaskService.SetAsCancelAsync(taskId);
        }
    }
}
