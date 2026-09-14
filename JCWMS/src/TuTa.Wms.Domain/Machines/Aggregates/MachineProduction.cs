using System;
using Volo.Abp.Domain.Entities;

namespace TuTa.Wms.Machines.Aggregates;

public class MachineProduction : AggregateRoot<int>
{
    /// <summary>
    /// 机台ID
    /// </summary>
    public int MachineId { get; set; }
    
    /// <summary>
    /// 物料代码
    /// </summary>
    public string MaterialCode { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>
    public string MaterialName { get; set; }
    
    /// <summary>
    /// 生产数量
    /// </summary>
    public int ProductionCount { get; set; }
    
    /// <summary>
    /// 生产状态
    /// </summary>
    public string ProductionStatus { get; set; }
    
    /// <summary>
    /// 开始生产时间
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束生产时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    public MachineProduction()
    {
    }

    public MachineProduction(int id, int machineId, string materialCode, string materialName)
        : base(id)
    {
        MachineId = machineId;
        MaterialCode = materialCode;
        MaterialName = materialName;
        ProductionCount = 0;
        ProductionStatus = "Running";
        StartTime = DateTime.Now;
    }
}