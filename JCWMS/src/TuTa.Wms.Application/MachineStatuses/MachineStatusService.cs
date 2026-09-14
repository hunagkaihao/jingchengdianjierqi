using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TuTa.Wms.Backgrounds;
using TuTa.Wms.Machines.Aggregates;
using TuTa.Wms.MachineStatuses.Dtos;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Wms.ConfigTool;
using Wms.RedisTool;

using Volo.Abp.Application.Services;

namespace TuTa.Wms.MachineStatuses
{
    public class MachineStatusService : ApplicationService, IMachineStatusService
    {
        private readonly IRedisClient _redisClient;
        private readonly IOptions<ConfigOptions> _options;
        private readonly ILogger<MachineStatusService> _logger;
        private readonly IRepository<Machine, int> _machineRepository;

        public MachineStatusService(
            IRedisClient redisClient,
            IOptions<ConfigOptions> options,
            ILogger<MachineStatusService> logger,
            IRepository<Machine, int> machineRepository)
        {
            _redisClient = redisClient;
            _options = options;
            _logger = logger;
            _machineRepository = machineRepository;
        }

        /// <summary>
        /// 获取所有机台门状态（来自 Redis Wms:MachineStatus）
        /// </summary>
        public Task<List<MachineStatusDto>> GetMachineStatusesAsync()
        {
            var result = new List<MachineStatusDto>();
            try
            {
                _redisClient.Build(_options.Value.RedisConnStr, _options.Value.DefaultRedisNo);

                var pairs = _redisClient.GetAllHashFieldValuePairs(MachineStatusBackgroundService.MachineStatusRedisKey);
                foreach (var pair in pairs)
                {
                    try
                    {
                        var status = JsonConvert.DeserializeObject<MachineStatusDto>(pair.Value);
                        if (status != null)
                        {
                            result.Add(status);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析机台状态失败, field: {Field}", pair.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从 Redis 读取机台状态失败");
            }

            // 按机台名、门号排序，便于前端展示
            result = result.OrderBy(m => m.MachineName).ThenBy(m => m.DoorNumber).ToList();
            return Task.FromResult(result);
        }

        /// <summary>
        /// 获取机台配置列表（用于启用/停用配置）
        /// </summary>
        public async Task<List<MachineConfigDto>> GetMachineConfigsAsync()
        {
            var machines = await _machineRepository.GetListAsync();
            return machines
                .OrderBy(m => m.MachineNumber)
                .ThenBy(m => m.Name)
                .Select(m => new MachineConfigDto
                {
                    Id = m.Id,
                    MachineName = m.Name,
                    Description = m.Description,
                    IpAddress = m.IpAddress,
                    Port = m.Port,
                    MachineNumber = m.MachineNumber,
                    IsEnabled = m.IsEnabled,
                })
                .ToList();
        }

        /// <summary>
        /// 设置机台是否启用（启用后机台状态监控与自动搬运任务在 10 秒内生效）
        /// </summary>
        public async Task SetMachineEnabledAsync(SetMachineEnabledInput input)
        {
            var machine = await _machineRepository.FindAsync(input.MachineId);
            if (machine == null)
            {
                throw new UserFriendlyException($"找不到机台, Id: {input.MachineId}");
            }

            if (machine.IsEnabled == input.IsEnabled)
            {
                return;
            }

            machine.IsEnabled = input.IsEnabled;
            await _machineRepository.UpdateAsync(machine);

            // 通知后台监控热更新配置（下一轮 10 秒循环内生效，无需重启服务）
            MachineStatusBackgroundService.RequestConfigReload();

            _logger.LogInformation("机台 {MachineName} 启用状态已变更为: {IsEnabled}", machine.Name, input.IsEnabled);
        }
    }
}
