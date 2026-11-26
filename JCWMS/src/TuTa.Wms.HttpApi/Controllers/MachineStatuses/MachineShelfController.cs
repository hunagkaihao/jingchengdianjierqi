using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTa.Wms.MachineStatuses;
using TuTa.Wms.MachineStatuses.Dtos;
using TuTa.Wms.Permissions;
using Volo.Abp.Uow;

namespace TuTa.Wms.Controllers.MachineStatuses;

/// <summary>PDA 机台货架绑定的读取和保存接口。</summary>
[ApiController]
[Route("wms/machine-shelves")]
[Authorize(WmsPermissions.EditPermission)]
public class MachineShelfController : WmsController
{
    private readonly IMachineShelfService _service;

    /// <summary>注入机台货架配置服务。</summary>
    public MachineShelfController(IMachineShelfService service)
    {
        _service = service;
    }

    /// <summary>获取当前货架绑定、参考点位和可选机台。</summary>
    [HttpGet]
    public Task<MachineShelvesDto> GetConfigurationAsync()
    {
        return _service.GetConfigurationAsync();
    }

    /// <summary>保存一个物理货架范围的机台绑定。</summary>
    [HttpPut("{shelfId:int}")]
    public Task<MachineShelfConfigDto> SetAssignmentAsync(int shelfId, SetMachineShelfInput input)
    {
        return _service.SetAssignmentAsync(shelfId, input);
    }

    /// <summary>读取机台货架范围内的实际库位记录。</summary>
    [HttpGet("cells")]
    public Task<List<MachineShelfCellDto>> GetShelfCellsAsync()
    {
        return _service.GetShelfCellsAsync();
    }

    /// <summary>新增一条机台货架库位记录。</summary>
    [HttpPost("cells")]
    [Authorize(WmsPermissions.AddPermission)]
    [UnitOfWork(true, IsolationLevel.Serializable)]
    public Task<MachineShelfCellDto> CreateShelfCellAsync(CreateMachineShelfCellInput input)
    {
        return _service.CreateShelfCellAsync(input);
    }

    /// <summary>保存选中的一条机台货架库位记录。</summary>
    [HttpPut("cells/{id:guid}")]
    public Task<MachineShelfCellDto> UpdateShelfCellAsync(Guid id, SetMachineShelfCellInput input)
    {
        return _service.UpdateShelfCellAsync(id, input);
    }

    /// <summary>删除选中的一条机台货架库位记录。</summary>
    [HttpDelete("cells/{id:guid}")]
    [Authorize(WmsPermissions.DeletePermission)]
    public Task DeleteShelfCellAsync(Guid id,
        [FromQuery, Required, StringLength(64, MinimumLength = 64)] string revision)
    {
        return _service.DeleteShelfCellAsync(id, revision);
    }
}
