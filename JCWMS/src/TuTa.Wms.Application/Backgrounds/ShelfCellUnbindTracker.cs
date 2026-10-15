using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks.Dto;
using TuTa.Wms.Cells;

namespace TuTa.Wms.Backgrounds;

/// <summary>
/// 在单个后台监控循环中跟踪货架取货及待解绑状态，避免持续无货时重复解绑。
/// 状态仅在当前进程内保存；应在库位状态提交成功后串行调用。
/// </summary>
public class ShelfCellUnbindTracker
{
    /// <summary>各库位上一次成功采集并提交的传感器状态。</summary>
    private readonly Dictionary<string, CellStatus> _lastStatuses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>取货后尚未确认解绑完成的库位，独立于数据库有无货状态。</summary>
    private readonly HashSet<string> _pendingCells = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 根据取货变化发起解绑；普通失败保留待重试，已无容器按完成处理。
    /// </summary>
    /// <param name="cellCode">完整仓位编码。</param>
    /// <param name="sensorStatus">当前传感器映射的有无货状态。</param>
    /// <param name="previousCellStatus">本轮更新前的数据库状态，用于首次采集判断取货。</param>
    /// <param name="unbind">按仓位执行 RCS 解绑并返回原始响应的操作。</param>
    /// <returns>当前库位的解绑处理任务；普通错误抛出异常，供后台记录并在后续循环重试。</returns>
    public async Task SyncAsync(string cellCode, CellStatus sensorStatus, CellStatus previousCellStatus,
        Func<string, Task<ResultAgvTaskDto>> unbind)
    {
        var previousStatus = _lastStatuses.TryGetValue(cellCode, out var lastStatus)
            ? lastStatus
            : previousCellStatus;
        _lastStatuses[cellCode] = sensorStatus;

        // 重新放货后取消旧取货的重试，避免解除新容器的绑定。
        if (sensorStatus != CellStatus.Nohave)
        {
            _pendingCells.Remove(cellCode);
            return;
        }

        if (previousStatus == CellStatus.Have || previousStatus == CellStatus.Full)
            _pendingCells.Add(cellCode);

        if (!_pendingCells.Contains(cellCode))
            return;

        var response = await unbind(cellCode);
        if (response?.Code == "0" || IsAlreadyEmpty(response?.Message, cellCode))
        {
            _pendingCells.Remove(cellCode);
            return;
        }

        throw new InvalidOperationException($"RCS 仓位解绑失败，返回码：{response?.Code ?? "空"}，消息：{response?.Message ?? "空响应"}");
    }

    /// <summary>
    /// 仅将明确表示仓位已无容器的业务消息视为幂等成功；其他错误仍需重试。
    /// </summary>
    private static bool IsAlreadyEmpty(string message, string cellCode)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        // 无现场错误码约定时采用明确的业务短语，不能将“容器不存在”等错误一概忽略。
        // 允许消息带当前仓位编号、括号和空白，不忽略其他仓位或其他业务错误。
        var normalized = Regex.Replace(message, @"[\s\[\]【】（）()]", string.Empty)
            .Replace(cellCode, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("仓位", "库位").Replace("库位上", "库位").Replace("库位已", "库位");
        return normalized.Contains("库位没有容器", StringComparison.Ordinal)
            || normalized.Contains("库位未绑定容器", StringComparison.Ordinal)
            || normalized.Contains("库位没有绑定容器", StringComparison.Ordinal)
            || normalized.Contains("库位无容器", StringComparison.Ordinal);
    }
}
