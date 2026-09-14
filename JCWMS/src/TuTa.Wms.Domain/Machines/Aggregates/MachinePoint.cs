using System;
using Volo.Abp.Domain.Entities;

namespace TuTa.Wms.Machines.Aggregates;

public class MachinePoint : AggregateRoot<int>
{
    /// <summary>
    /// 机台ID
    /// </summary>
    public int MachineId { get; set; }
    
    /// <summary>
    /// 点名称
    /// </summary>
    public string Name { get; set; }
    

    
    /// <summary>
    /// 门编号（1-4）
    /// </summary>
    public int DoorNumber { get; set; }
    

    
    /// <summary>
    /// 库位代码
    /// </summary>
    public string CellCode { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// 满料信号地址
    /// </summary>
    public ushort FullMaterialAddress { get; set; }
    
    /// <summary>
    /// 治具伸出信号地址
    /// </summary>
    public ushort JigExtendAddress { get; set; }
    
    /// <summary>
    /// 完成信号地址
    /// </summary>
    public ushort CompleteAddress { get; set; }
    
    /// <summary>
    /// 一体机小车到位信号地址
    /// </summary>
    public ushort ArriveAddress { get; set; }
    
    /// <summary>
    /// 机台IP地址
    /// </summary>
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; } = 502;
    
    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; set; } = 1;
    
    /// <summary>
    /// 任务类型
    /// </summary>
    public string TaskType { get; set; }

    public MachinePoint()
    {
    }

    public MachinePoint(int id, int machineId, string name, int doorNumber, string cellCode, string ipAddress, int port, byte slaveId, ushort completeAddress, string taskType)
        : base(id)
    {
        MachineId = machineId;
        Name = name;
        DoorNumber = doorNumber;
        CellCode = cellCode;
        IpAddress = ipAddress;
        Port = port;
        SlaveId = slaveId;
        CompleteAddress = completeAddress;
        TaskType = taskType;
    }
}