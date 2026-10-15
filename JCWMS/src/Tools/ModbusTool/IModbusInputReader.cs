using System.Threading;
using System.Threading.Tasks;

namespace Wms.ModbusTool;

/// <summary>
/// 读取 Modbus 离散输入状态的服务。
/// </summary>
public interface IModbusInputReader
{
    /// <summary>
    /// 读取指定范围的离散输入，并保留通信是否成功的信息。
    /// </summary>
    /// <param name="ipAddress">设备 IPv4 地址。</param>
    /// <param name="port">设备 TCP 端口。</param>
    /// <param name="slaveId">Modbus 单元标识。</param>
    /// <param name="startAddress">离散输入起始地址。</param>
    /// <param name="count">读取点数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>读取结果；失败时 <see cref="ModbusInputReadResult.IsSuccess"/> 为 false。</returns>
    Task<ModbusInputReadResult> ReadInputsWithStatusAsync(string ipAddress, int port, byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default);
}

/// <summary>
/// Modbus 离散输入读取结果，避免通信失败被误判为全部输入为零。
/// </summary>
public sealed record ModbusInputReadResult(bool IsSuccess, bool[] Values, string ErrorMessage)
{
    /// <summary>创建成功读取结果。</summary>
    public static ModbusInputReadResult Success(bool[] values) => new(true, values, null);

    /// <summary>创建失败读取结果。</summary>
    public static ModbusInputReadResult Failure(int count, string errorMessage) => new(false, new bool[count], errorMessage);
}
