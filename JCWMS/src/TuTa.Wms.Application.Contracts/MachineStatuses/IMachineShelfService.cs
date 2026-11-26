using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TuTa.Wms.MachineStatuses.Dtos;
using Volo.Abp.Application.Services;

namespace TuTa.Wms.MachineStatuses;

/// <summary>读取和编辑 PDA 机台货架绑定。</summary>
public interface IMachineShelfService : IApplicationService
{
    /// <summary>读取现场范围、数据库当前绑定和可选机台。</summary>
    Task<MachineShelvesDto> GetConfigurationAsync();

    /// <summary>按范围首列编号保存该范围内所有列和层的机台绑定。</summary>
    Task<MachineShelfConfigDto> SetAssignmentAsync(int shelfId, SetMachineShelfInput input);

    /// <summary>按实际库位记录列出机台货架。</summary>
    Task<List<MachineShelfCellDto>> GetShelfCellsAsync();

    /// <summary>新增一个物理点位或为已有点位新增一条货架绑定。</summary>
    Task<MachineShelfCellDto> CreateShelfCellAsync(CreateMachineShelfCellInput input);

    /// <summary>按库位记录 ID 保存一行机台货架数据。</summary>
    Task<MachineShelfCellDto> UpdateShelfCellAsync(Guid id, SetMachineShelfCellInput input);

    /// <summary>按记录 ID 和当前版本删除一条机台货架库位记录。</summary>
    Task DeleteShelfCellAsync(Guid id, string revision);
}
