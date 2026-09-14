using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TuTa.Wms.AgvTasks
{
    public interface IAgvTaskService:IApplicationService
    {

        /// <summary>
        /// CTU回调接口
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<ResultAgvTaskDto> CtuCallbackAsync(AgvCallBackRequest input);

        /// <summary>
        /// 获取AGV任务清单
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<PagedResultDto<AgvTaskDto>> GetPagingListAsync(PagingAgvTaskListInput input);

        /// <summary>
        /// 将任务设置为执行中
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        Task SetAsExecutingAsync(int taskId);

        /// <summary>
        /// 将任务设置为完成
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        Task SetAsCompletedAsync(int taskId);

        /// <summary>
        /// 将任务设置为取消
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="isSync"></param>
        /// <returns></returns>
        Task SetAsCancelAsync(int taskId);
        
        /// <summary>
        /// 空盒衬上机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <returns></returns>
        Task CreateEmptyBoxToMachineTaskAsync(string cellCode);
        
        /// <summary>
        /// 成品下机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <param name="machineName">机台名称</param>
        /// <returns></returns>
        Task CreateFinishedFromMachineTaskAsync(string cellCode, string machineName);
        
        /// <summary>
        /// 空盒衬下机台任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <returns></returns>
        Task CreateEmptyBoxFromMachineTaskAsync(string cellCode);
        
        /// <summary>
        /// 半成品上料任务
        /// </summary>
        /// <param name="cellCode">库位编码</param>
        /// <param name="machineName">机台名称</param>
        /// <returns></returns>
        Task CreateSemiFinishedFromMachineTaskAsync(string cellCode, string machineName);
    }
}
