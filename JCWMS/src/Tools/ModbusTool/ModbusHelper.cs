using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Wms.ConfigTool;
using Wms.LogTool;

namespace Wms.ModbusTool;

public class ModbusHelper : ISingletonDependency, IModbusInputReader
{
    private readonly ILogger<ModbusHelper> _logger;
    private readonly IOptions<ConfigOptions> _options;

    public ModbusHelper(
        IOptions<ConfigOptions> options,
        ILogger<ModbusHelper> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 读取机台的下料和上料状态
    /// </summary>
    /// <param name="ipAddress">机台IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="slaveId">从站ID</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <returns>状态数组</returns>
    /// <summary>读取离散输入并返回明确的通信结果。</summary>
    public async Task<ModbusInputReadResult> ReadInputsWithStatusAsync(string ipAddress, int port, byte slaveId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            await ConnectWithTimeoutAsync(client, ipAddress, port, cancellationToken);
            stream = client.GetStream();
            var request = BuildReadInputsRequest(slaveId, startAddress, count);
            await SendWithTimeoutAsync(stream, request, cancellationToken);
            var response = new byte[1024];
            var bytesRead = await ReceiveWithTimeoutAsync(stream, response, cancellationToken);
            var expectedByteCount = (count + 7) / 8;
            if (bytesRead < 9 || response[7] != 0x02 || response[8] != expectedByteCount || bytesRead < 9 + expectedByteCount)
                return ModbusInputReadResult.Failure(count, "Modbus 响应格式错误");
            return ModbusInputReadResult.Success(ParseReadInputsResponse(response, bytesRead, count));
        }
        catch (Exception ex)
        {
            _logger.Error($"读取离散输入失败 [{ipAddress}:{port}]: {ex.Message}");
            return ModbusInputReadResult.Failure(count, ex.Message);
        }
        finally { stream?.Close(); client?.Close(); }
    }

    public async Task<bool[]> ReadMachineStatusAsync(string ipAddress, int port, byte slaveId, ushort startAddress, ushort count)
    {
        return (await ReadInputsWithStatusAsync(ipAddress, port, slaveId, startAddress, count)).Values;
        /*
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            using (var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await client.ConnectAsync(ipAddress, port, connectCts.Token);
            }
            client.ReceiveTimeout = 5000;
            client.SendTimeout = 3000;
            stream = client.GetStream();

            // 构建Modbus TCP请求
            byte[] request = BuildReadInputsRequest(slaveId, startAddress, count);
            await stream.WriteAsync(request, 0, request.Length);

            // 读取响应
            byte[] response = new byte[1024];
            int bytesRead = await stream.ReadAsync(response, 0, response.Length);

            // 解析响应
            bool[] result = ParseReadInputsResponse(response, bytesRead, count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取机台状态失败 [{ipAddress}:{port}]: {ex.Message}");
            return new bool[count];
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }*/
    }

    /// <summary>
    /// 读取机台的模拟量数据
    /// </summary>
    /// <param name="ipAddress">机台IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="slaveId">从站ID</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <returns>模拟量数据</returns>
    public async Task<ushort[]> ReadMachineAnalogAsync(string ipAddress, int port, byte slaveId, ushort startAddress, ushort count)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            await ConnectWithTimeoutAsync(client, ipAddress, port);
            stream = client.GetStream();

            // 构建Modbus TCP请求（读取保持寄存器，功能码0x03）
            byte[] request = BuildReadHoldingRegistersRequest(slaveId, startAddress, count);
            await SendWithTimeoutAsync(stream, request);

            // 读取响应
            byte[] response = new byte[1024];
            int bytesRead = await ReceiveWithTimeoutAsync(stream, response);

            // 解析响应
            ushort[] result = ParseReadHoldingRegistersResponse(response, bytesRead, count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取机台模拟量失败 [{ipAddress}:{port}]: {ex.Message}");
            return new ushort[count];
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    /// <summary>
    /// 读取单个线圈状态
    /// </summary>
    /// <param name="ipAddress">机台IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="slaveId">从站ID</param>
    /// <param name="address">地址</param>
    /// <returns>线圈状态</returns>
    public async Task<bool> ReadCoilAsync(string ipAddress, int port, byte slaveId, ushort address)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            await ConnectWithTimeoutAsync(client, ipAddress, port);
            stream = client.GetStream();

            // 构建Modbus TCP请求（读取线圈状态，功能码0x01）
            byte[] request = new byte[12];
            // 事务标识符
            request[0] = 0x00;
            request[1] = 0x04;
            // 协议标识符
            request[2] = 0x00;
            request[3] = 0x00;
            // 长度
            request[4] = 0x00;
            request[5] = 0x06;
            // 从站地址
            request[6] = slaveId;
            // 功能码
            request[7] = 0x01;
            // 起始地址
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            // 数量
            request[10] = 0x00;
            request[11] = 0x01;
            await SendWithTimeoutAsync(stream, request);

            // 读取响应
            byte[] response = new byte[1024];
            int bytesRead = await ReceiveWithTimeoutAsync(stream, response);

            // 解析响应
            if (bytesRead >= 9)
            {
                byte byteCount = response[8];
                if (byteCount > 0 && 9 < bytesRead)
                {
                    return ((response[9] >> 0) & 0x01) == 0x01;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取线圈状态失败 [{ipAddress}:{port}]: {ex.Message}");
            return false;
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    /// <summary>
    /// 读取单个线圈状态（可区分断线与正常值为false）
    /// </summary>
    /// <param name="ipAddress">机台IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="slaveId">从站ID</param>
    /// <param name="address">地址</param>
    /// <returns>Success：是否成功读取到响应（false 表示连接失败/超时等异常）；Value：线圈状态</returns>
    public async Task<(bool Success, bool Value)> ReadCoilWithStatusAsync(string ipAddress, int port, byte slaveId, ushort address)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            await ConnectWithTimeoutAsync(client, ipAddress, port);
            stream = client.GetStream();

            // 构建Modbus TCP请求（读取线圈状态，功能码0x01）
            byte[] request = new byte[12];
            // 事务标识符
            request[0] = 0x00;
            request[1] = 0x04;
            // 协议标识符
            request[2] = 0x00;
            request[3] = 0x00;
            // 长度
            request[4] = 0x00;
            request[5] = 0x06;
            // 从站地址
            request[6] = slaveId;
            // 功能码
            request[7] = 0x01;
            // 起始地址
            request[8] = (byte)(address >> 8);
            request[9] = (byte)(address & 0xFF);
            // 数量
            request[10] = 0x00;
            request[11] = 0x01;
            await SendWithTimeoutAsync(stream, request);

            // 读取响应
            byte[] response = new byte[1024];
            int bytesRead = await ReceiveWithTimeoutAsync(stream, response);

            // 解析响应
            if (bytesRead >= 9)
            {
                byte byteCount = response[8];
                if (byteCount > 0 && 9 < bytesRead)
                {
                    return (true, ((response[9] >> 0) & 0x01) == 0x01);
                }
            }
            // 响应格式异常也视为读取失败
            return (false, false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("读取线圈超时 [{Ip}:{Port}]，地址: {Address}，原因: {Reason}", ipAddress, port, address, ex.Message);
            return (false, false);
        }
        catch (Exception)
        {
            // 连接失败/超时/读写异常，均视为断线
            return (false, false);
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    /// <summary>
    /// 读取多个线圈状态
    /// </summary>
    /// <param name="ipAddress">机台IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="slaveId">从站ID</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <returns>线圈状态数组</returns>
    public async Task<bool[]> ReadCoilsAsync(string ipAddress, int port, byte slaveId, ushort startAddress, ushort count)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            await ConnectWithTimeoutAsync(client, ipAddress, port);
            stream = client.GetStream();

            // 构建Modbus TCP请求（读取线圈状态，功能码0x01）
            byte[] request = new byte[12];
            // 事务标识符
            request[0] = 0x00;
            request[1] = 0x06;
            // 协议标识符
            request[2] = 0x00;
            request[3] = 0x00;
            // 长度
            request[4] = 0x00;
            request[5] = 0x06;
            // 从站地址
            request[6] = slaveId;
            // 功能码
            request[7] = 0x01;
            // 起始地址
            request[8] = (byte)(startAddress >> 8);
            request[9] = (byte)(startAddress & 0xFF);
            // 数量
            request[10] = (byte)(count >> 8);
            request[11] = (byte)(count & 0xFF);
            await SendWithTimeoutAsync(stream, request);

            // 读取响应
            byte[] response = new byte[1024];
            int bytesRead = await ReceiveWithTimeoutAsync(stream, response);

            // 解析响应
            bool[] result = new bool[count];
            if (bytesRead >= 9)
            {
                byte byteCount = response[8];
                for (int i = 0; i < count; i++)
                {
                    int byteIndex = 9 + (i / 8);
                    int bitIndex = i % 8;
                    if (byteIndex < bytesRead)
                    {
                        result[i] = ((response[byteIndex] >> bitIndex) & 0x01) == 0x01;
                    }
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取多个线圈状态失败 [{ipAddress}:{port}]: {ex.Message}");
            return new bool[count];
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    /// <summary>
    /// 写入机台控制指令
    /// </summary>
    /// <param name="ipAddress">机台IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="slaveId">从站ID</param>
    /// <param name="address">地址</param>
    /// <param name="value">值</param>
    /// <returns>是否成功</returns>
    public async Task<bool> WriteMachineCommandAsync(string ipAddress, int port, byte slaveId, ushort address, bool value)
    {
        TcpClient client = null;
        NetworkStream stream = null;
        try
        {
            client = new TcpClient();
            await ConnectWithTimeoutAsync(client, ipAddress, port);
            stream = client.GetStream();

            // 构建Modbus TCP请求
            byte[] request = BuildWriteSingleCoilRequest(slaveId, address, value);
            await SendWithTimeoutAsync(stream, request);

            // 读取响应
            byte[] response = new byte[1024];
            int bytesRead = await ReceiveWithTimeoutAsync(stream, response);

            // 检查响应是否成功
            return ParseWriteSingleCoilResponse(response, bytesRead);
        }
        catch (Exception ex)
        {
            _logger.Error($"写入机台指令失败 [{ipAddress}:{port}]: {ex.Message}");
            return false;
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    /// <summary>PLC 每次连接的超时，单位为毫秒。</summary>
    private const int ConnectTimeoutMs = 5000;

    /// <summary>PLC 每次发送请求的超时，单位为毫秒。</summary>
    private const int SendTimeoutMs = 3000;

    /// <summary>PLC 每次等待响应的超时，单位为毫秒。</summary>
    private const int ReceiveTimeoutMs = 5000;

    /// <summary>
    /// 限时建立连接；调用方取消仍按取消处理，内部超时转换为含阶段信息的异常。
    /// </summary>
    /// <param name="client">由调用方负责关闭的 TCP 客户端。</param>
    /// <param name="ipAddress">PLC 地址。</param>
    /// <param name="port">PLC 端口。</param>
    /// <param name="cancellationToken">调用方取消令牌，默认不主动取消。</param>
    private static async Task ConnectWithTimeoutAsync(TcpClient client, string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ConnectTimeoutMs);
        try
        {
            await client.ConnectAsync(ipAddress, port, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"PLC 连接超时（{ConnectTimeoutMs} 毫秒）", ex);
        }
    }

    /// <summary>使用独立超时令牌发送请求，避免异步写入无限等待。</summary>
    /// <param name="stream">已连接的网络流，由调用方负责关闭。</param>
    /// <param name="request">完整请求报文。</param>
    /// <param name="cancellationToken">调用方取消令牌，默认不主动取消。</param>
    private static async Task SendWithTimeoutAsync(NetworkStream stream, byte[] request, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SendTimeoutMs);
        try
        {
            await stream.WriteAsync(request, 0, request.Length, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"PLC 发送超时（{SendTimeoutMs} 毫秒）", ex);
        }
    }

    /// <summary>使用独立超时令牌等待响应，避免 PLC 接受连接后不回包拖住轮询。</summary>
    /// <param name="stream">已连接的网络流，由调用方负责关闭。</param>
    /// <param name="response">接收缓冲区。</param>
    /// <param name="cancellationToken">调用方取消令牌，默认不主动取消。</param>
    /// <returns>实际收到的字节数；对端关闭连接时可能为 0。</returns>
    private static async Task<int> ReceiveWithTimeoutAsync(NetworkStream stream, byte[] response, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ReceiveTimeoutMs);
        try
        {
            return await stream.ReadAsync(response, 0, response.Length, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"PLC 接收超时（{ReceiveTimeoutMs} 毫秒）", ex);
        }
    }

    // 构建读取离散输入的请求
    private byte[] BuildReadInputsRequest(byte slaveId, ushort startAddress, ushort count)
    {
        byte[] request = new byte[12];
        // 事务标识符
        request[0] = 0x00;
        request[1] = 0x01;
        // 协议标识符
        request[2] = 0x00;
        request[3] = 0x00;
        // 长度
        request[4] = 0x00;
        request[5] = 0x06;
        // 从站地址
        request[6] = slaveId;
        // 功能码
        request[7] = 0x02;
        // 起始地址
        request[8] = (byte)(startAddress >> 8);
        request[9] = (byte)(startAddress & 0xFF);
        // 数量
        request[10] = (byte)(count >> 8);
        request[11] = (byte)(count & 0xFF);
        return request;
    }

    // 解析读取离散输入的响应
    private bool[] ParseReadInputsResponse(byte[] response, int bytesRead, int count)
    {
        bool[] result = new bool[count];
        if (bytesRead < 9)
        {
            return result;
        }

        byte byteCount = response[8];
        for (int i = 0; i < count; i++)
        {
            int byteIndex = 9 + (i / 8);
            int bitIndex = i % 8;
            if (byteIndex < bytesRead)
            {
                result[i] = ((response[byteIndex] >> bitIndex) & 0x01) == 0x01;
            }
        }
        return result;
    }

    // 构建读取保持寄存器的请求
    private byte[] BuildReadHoldingRegistersRequest(byte slaveId, ushort startAddress, ushort count)
    {
        byte[] request = new byte[12];
        // 事务标识符
        request[0] = 0x00;
        request[1] = 0x02;
        // 协议标识符
        request[2] = 0x00;
        request[3] = 0x00;
        // 长度
        request[4] = 0x00;
        request[5] = 0x06;
        // 从站地址
        request[6] = slaveId;
        // 功能码
        request[7] = 0x03;
        // 起始地址
        request[8] = (byte)(startAddress >> 8);
        request[9] = (byte)(startAddress & 0xFF);
        // 数量
        request[10] = (byte)(count >> 8);
        request[11] = (byte)(count & 0xFF);
        return request;
    }

    // 解析读取保持寄存器的响应
    private ushort[] ParseReadHoldingRegistersResponse(byte[] response, int bytesRead, int count)
    {
        ushort[] result = new ushort[count];
        if (bytesRead < 9)
        {
            return result;
        }

        byte byteCount = response[8];
        for (int i = 0; i < count; i++)
        {
            int byteIndex = 9 + (i * 2);
            if (byteIndex + 1 < bytesRead)
            {
                result[i] = (ushort)((response[byteIndex] << 8) | response[byteIndex + 1]);
            }
        }
        return result;
    }

    // 构建写入单个线圈的请求
    private byte[] BuildWriteSingleCoilRequest(byte slaveId, ushort address, bool value)
    {
        byte[] request = new byte[12];
        // 事务标识符
        request[0] = 0x00;
        request[1] = 0x03;
        // 协议标识符
        request[2] = 0x00;
        request[3] = 0x00;
        // 长度
        request[4] = 0x00;
        request[5] = 0x06;
        // 从站地址
        request[6] = slaveId;
        // 功能码
        request[7] = 0x05;
        // 地址
        request[8] = (byte)(address >> 8);
        request[9] = (byte)(address & 0xFF);
        // 值
        request[10] = value ? (byte)0xFF : (byte)0x00;
        request[11] = 0x00;
        return request;
    }

    // 解析写入单个线圈的响应
    private bool ParseWriteSingleCoilResponse(byte[] response, int bytesRead)
    {
        if (bytesRead < 12)
        {
            return false;
        }

        // 检查功能码
        return response[7] == 0x05;
    }
}
