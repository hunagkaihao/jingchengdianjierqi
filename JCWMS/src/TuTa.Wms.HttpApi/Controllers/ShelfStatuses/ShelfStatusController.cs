using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wms.ModbusTool;
using Wms.ConfigTool;
using Microsoft.Extensions.Options;

namespace TuTa.Wms.Controllers.ShelfStatuses;

/// <summary>
/// 提供现场固定货架的实时有货状态查询。
/// </summary>
[Route("wms/shelf-status")]
[ApiController]
public class ShelfStatusController : WmsController
{
    private readonly IModbusInputReader _modbusInputReader;
    private readonly ConfigOptions _options;

    /// <summary>
    /// 初始化货架状态控制器。
    /// </summary>
    public ShelfStatusController(IModbusInputReader modbusInputReader, IOptions<ConfigOptions> options) { _modbusInputReader = modbusInputReader; _options = options.Value; }

    /// <summary>
    /// 读取两排货架的当前状态；DI 为 0 表示有货，1 表示无货。
    /// </summary>
    [HttpGet("current")]
    public async Task<ShelfStatusResponseDto> GetCurrentAsync()
    {
        var devices = _options.ShelfStatusDevices;
        var reads = await Task.WhenAll(devices.Select(device => _modbusInputReader.ReadInputsWithStatusAsync(device.IpAddress, device.Port, device.SlaveId, 0, device.ReadCount)));
        return new ShelfStatusResponseDto
        {
            RefreshedAt = DateTime.Now,
            Rows = devices.Select((device, index) => CreateRow(device.RowName, $"{device.IpAddress}:{device.Port}", reads[index], device.ShelfRanges.Select(range => new ShelfRange(range.Start, range.End, range.Step, range.TypeCode)))).ToList(),
        };
    }

    private static ShelfStatusRowDto CreateRow(string rowName, string address, ModbusInputReadResult result, IEnumerable<ShelfRange> ranges)
    {
        var row = new ShelfStatusRowDto { RowName = rowName, DeviceAddress = address, IsSuccess = result.IsSuccess, ErrorMessage = result.ErrorMessage };
        if (!result.IsSuccess) return row;
        foreach (var range in ranges)
            for (var shelf = range.Start; shelf != range.End + range.Step; shelf += range.Step)
                for (var layer = 1; layer <= 3; layer++)
                {
                    var index = row.Layers.Count;
                    row.Layers.Add(new ShelfStatusLayerDto { ShelfNumber = shelf.ToString("D6"), ShelfCode = $"{shelf:D6}X{range.TypeCode}{layer:D2}013", Layer = layer, DiIndex = index + 1, Status = result.Values[index] ? 1 : 0 });
                }
        return row;
    }

    private sealed record ShelfRange(int Start, int End, int Step, string TypeCode);
}

/// <summary>货架状态查询响应。</summary>
public class ShelfStatusResponseDto { public DateTime RefreshedAt { get; set; } public List<ShelfStatusRowDto> Rows { get; set; } = new(); }
/// <summary>一排货架的采集结果。</summary>
public class ShelfStatusRowDto { public string RowName { get; set; } public string DeviceAddress { get; set; } public bool IsSuccess { get; set; } public string ErrorMessage { get; set; } public List<ShelfStatusLayerDto> Layers { get; set; } = new(); }
/// <summary>单个货架层位状态。</summary>
public class ShelfStatusLayerDto { public string ShelfCode { get; set; } public string ShelfNumber { get; set; } public int Layer { get; set; } public int DiIndex { get; set; } public int Status { get; set; } }
