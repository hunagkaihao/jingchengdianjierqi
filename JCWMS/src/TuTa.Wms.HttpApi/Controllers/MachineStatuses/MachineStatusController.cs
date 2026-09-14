using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TuTa.Wms.MachineStatuses;
using TuTa.Wms.MachineStatuses.Dtos;

namespace TuTa.Wms.Controllers.MachineStatuses
{
    [Route("wms/machine-status")]
    [ApiController]
    public class MachineStatusController : WmsController
    {
        private readonly IMachineStatusService _machineStatusService;

        public MachineStatusController(IMachineStatusService machineStatusService)
        {
            _machineStatusService = machineStatusService;
        }

        [HttpGet("statuses")]
        [SwaggerOperation(summary: "获取所有机台门状态", Tags = new[] { "MachineStatus" })]
        public async Task<List<MachineStatusDto>> GetMachineStatusesAsync()
        {
            return await _machineStatusService.GetMachineStatusesAsync();
        }

        [HttpGet("machines")]
        [SwaggerOperation(summary: "获取机台配置列表", Tags = new[] { "MachineStatus" })]
        public async Task<List<MachineConfigDto>> GetMachineConfigsAsync()
        {
            return await _machineStatusService.GetMachineConfigsAsync();
        }

        [HttpPost("setMachineEnabled")]
        [SwaggerOperation(summary: "设置机台是否启用", Tags = new[] { "MachineStatus" })]
        public async Task SetMachineEnabledAsync(SetMachineEnabledInput input)
        {
            await _machineStatusService.SetMachineEnabledAsync(input);
        }
    }
}
