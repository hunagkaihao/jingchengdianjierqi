using System;
using Volo.Abp.Domain.Entities;

namespace TuTa.Wms.Machines.Aggregates;

public class Machine : AggregateRoot<int>
{
    /// <summary>
    /// 机台名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; } = 502;
    
    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; set; } = 10;
    
    /// <summary>
    /// 机台编号（1-3）
    /// </summary>
    public int MachineNumber { get; set; }
    
    /// <summary>
    /// 门数量（默认4）
    /// </summary>
    public int DoorCount { get; set; } = 4;
    
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// 是否启用自动搬运
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    


    public Machine()
    {
    }

    public Machine(int id, string name, string ipAddress, int port, byte slaveId, int machineNumber, bool isEnabled = true)
        : base(id)
    {
        Name = name;
        IpAddress = ipAddress;
        Port = port;
        SlaveId = slaveId;
        MachineNumber = machineNumber;
        IsEnabled = isEnabled;
    }
}