using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TuTa.Wms.AgvTasks.Dto;

namespace TuTa.Wms.AgvTasks;

/// <summary>对明确无可用仓位的任务下发失败，执行一次目标仓位解绑及原任务重发。</summary>
internal static class RcsTaskDispatchRecovery
{
    /// <summary>按原任务及目标仓位标识一次恢复解绑，同一操作重复提交时编号保持一致。</summary>
    /// <param name="taskRequestCode">原搬运任务的唯一请求编号。</param>
    /// <param name="targetCellCode">该任务需要解绑的目标仓位编码。</param>
    /// <returns>32 位十六进制编号，与搬运请求编号区分，并在重复提交时保持稳定。</returns>
    internal static string GetUnbindRequestCode(string taskRequestCode, string targetCellCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskRequestCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCellCode);
        // 将操作类型和字段长度纳入摘要，防止不同任务、仓位组合共用请求编号。
        var operation = $"target-unbind:{taskRequestCode.Length}:{taskRequestCode}:{targetCellCode.Length}:{targetCellCode}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(operation));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }

    /// <summary>返回最终下发响应；解绑失败抛出异常，不继续下发。</summary>
    /// <param name="targetCellCode">原任务的目标仓位编码，不含路径后缀。</param>
    /// <param name="dispatch">下发原任务的操作，恢复时最多额外调用一次。</param>
    /// <param name="unbind">按目标编码调用 RCS 解绑并返回原始响应。</param>
    /// <param name="log">记录恢复步骤的日志操作。</param>
    internal static async Task<ResultAgvTaskDto> SendAsync(string targetCellCode,
        Func<Task<ResultAgvTaskDto>> dispatch, Func<string, Task<ResultAgvTaskDto>> unbind, Action<string> log)
    {
        var response = await dispatch();
        // 只对现场确认的业务拒绝执行恢复；空响应、通信异常及其他错误不自动解绑。
        if (response == null || string.IsNullOrWhiteSpace(response.Code) || response.Code == "0"
            || response.Message?.Contains("未找到可用仓位", StringComparison.Ordinal) != true
            || string.IsNullOrWhiteSpace(targetCellCode))
            return response;

        log($"RCS 返回未找到可用仓位，开始解绑目标仓位 {targetCellCode}");
        var unbindResponse = await unbind(targetCellCode);
        if (unbindResponse?.Code != "0" && !IsAlreadyEmpty(unbindResponse?.Message, targetCellCode))
            throw new InvalidOperationException($"目标仓位 {targetCellCode} 解绑失败，停止任务重发。返回码：{unbindResponse?.Code ?? "空"}，消息：{unbindResponse?.Message ?? "空响应"}");

        log($"目标仓位 {targetCellCode} 已完成解绑或已无容器，重发原任务一次");
        // 不递归调用恢复流程，第二次失败直接交回调用方处理。
        response = await dispatch();
        log($"目标仓位 {targetCellCode} 原任务重发结束，返回码：{response?.Code ?? "空"}，消息：{response?.Message ?? "空响应"}");
        return response;
    }

    /// <summary>识别目标仓位已无容器的明确业务响应，允许继续唯一一次任务重发。</summary>
    private static bool IsAlreadyEmpty(string message, string targetCellCode)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var normalized = Regex.Replace(message, @"[\s\[\]【】（）()]", string.Empty)
            .Replace(targetCellCode, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("仓位", "库位").Replace("库位上", "库位").Replace("库位已", "库位");
        return normalized.Contains("库位没有容器", StringComparison.Ordinal)
            || normalized.Contains("库位未绑定容器", StringComparison.Ordinal)
            || normalized.Contains("库位没有绑定容器", StringComparison.Ordinal)
            || normalized.Contains("库位无容器", StringComparison.Ordinal);
    }
}
