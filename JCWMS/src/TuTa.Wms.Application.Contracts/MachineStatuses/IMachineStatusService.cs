using System.Collections.Generic;
using System.Threading.Tasks;
using TuTa.Wms.MachineStatuses.Dtos;

using Volo.Abp.Application.Services;

namespace TuTa.Wms.MachineStatuses
{
    public interface IMachineStatusService : IApplicationService
    {
        /// <summary>
        /// 获取所有机台门状态（来自 Redis）
        /// </summary>
        Task<List<MachineStatusDto>> GetMachineStatusesAsync();

        /// <summary>
        /// 获取机台配置列表（用于启用/停用配置）
        /// </summary>
        Task<List<MachineConfigDto>> GetMachineConfigsAsync();

        /// <summary>
        /// 设置机台是否启用（启用后机台状态监控与自动搬运任务在 10 秒内生效）
        /// </summary>
        Task SetMachineEnabledAsync(SetMachineEnabledInput input);
    }
}
