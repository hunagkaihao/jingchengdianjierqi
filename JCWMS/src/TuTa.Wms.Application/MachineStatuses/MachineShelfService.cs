using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TuTa.Wms.Cells;
using TuTa.Wms.Cells.Aggregates;
using TuTa.Wms.Machines.Aggregates;
using TuTa.Wms.MachineStatuses.Dtos;
using TuTa.Wms.Permissions;
using TuTa.Wms.Warehouses;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace TuTa.Wms.MachineStatuses;

/// <summary>维护 PDA 的机台货架绑定，同一物理范围内的所有库位记录一起更新。</summary>
[Authorize(WmsPermissions.EditPermission)]
public class MachineShelfService : ApplicationService, IMachineShelfService
{
    /// <summary>现场点位表中的货架范围。同一列包含三层，区域编号与自动选位规则一致。</summary>
    private sealed record ShelfRange(int Start, int End, int AreaId, string Kind, string ReferenceMachines)
    {
        /// <summary>空盒衬机台名称的方向后缀，成品和半成品使用原机台名称。</summary>
        public string Suffix => Kind switch { "feed" => "-1", "return" => "-2", _ => "" };
    }

    /// <summary>现场提供的货架点位和参考机台。读取页面时仅供比对，保存前以库位现有绑定为准。</summary>
    private static readonly ShelfRange[] Ranges =
    {
        new(600001, 600001, 1, "feed", "C18"),
        new(600002, 600002, 1, "feed", "C15,C17,C20,C22,C02,C03,C95"),
        new(600003, 600003, 1, "feed", "C13,C35,C23,C21"),
        new(700004, 700006, 2, "finished", "C20"),
        new(700007, 700009, 2, "finished", "C95"),
        new(700010, 700012, 2, "finished", "C02"),
        new(700013, 700015, 2, "finished", "C03"),
        new(800013, 800015, 2, "finished", "C18"),
        new(800010, 800012, 2, "finished", "C15"),
        new(800019, 800021, 2, "finished", "C13"),
        new(800025, 800027, 2, "finished", "C17"),
        new(700001, 700003, 3, "semiFinished", "C20"),
        new(700025, 700027, 3, "semiFinished", "C35"),
        new(800016, 800018, 3, "semiFinished", "C36"),
        new(800022, 800024, 3, "semiFinished", "C21"),
        new(800028, 800030, 3, "semiFinished", "C23"),
        new(800001, 800003, 3, "semiFinished", "C17"),
        new(800004, 800006, 3, "semiFinished", "C13"),
        new(800007, 800009, 3, "semiFinished", "C15"),
        new(700022, 700024, 1, "return", "C13,C15,C17,C20,C22"),
    };

    private readonly ICellRepository _cellRepository;
    private readonly IRepository<Machine, int> _machineRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    /// <summary>使用 WMS 库位和机台主数据读取、校验及持久化绑定。</summary>
    public MachineShelfService(ICellRepository cellRepository, IRepository<Machine, int> machineRepository,
        IWarehouseRepository warehouseRepository)
    {
        _cellRepository = cellRepository;
        _machineRepository = machineRepository;
        _warehouseRepository = warehouseRepository;
    }

    /// <summary>读取现场范围、数据库当前绑定和可选机台。</summary>
    public async Task<MachineShelvesDto> GetConfigurationAsync()
    {
        var machines = await GetMachineNamesAsync();
        var cells = await GetCellsAsync(Ranges);
        return new MachineShelvesDto
        {
            MachineNames = machines,
            Shelves = Ranges.Select(range => ToDto(range, GetRangeCells(range, cells), machines)).ToList()
        };
    }

    /// <summary>在同一个事务中更新该范围内全部现有库位的机台绑定。</summary>
    [UnitOfWork(true)]
    public async Task<MachineShelfConfigDto> SetAssignmentAsync(int shelfId, SetMachineShelfInput input)
    {
        var range = Ranges.FirstOrDefault(r => r.Start == shelfId);
        if (range == null)
            throw new UserFriendlyException("未找到可配置的货架范围");

        var machines = await GetMachineNamesAsync();
        var cells = GetRangeCells(range, await GetCellsAsync(new[] { range }));
        var current = ToDto(range, cells, machines);
        if (current.Revision != input.Revision)
            throw new UserFriendlyException("货架配置已发生变化，请刷新后再修改");
        if (current.MissingLayers > 0)
            throw new UserFriendlyException("该范围存在未配置的库位层，请先补齐库位");

        var requested = new HashSet<string>(input.MachineNames, StringComparer.OrdinalIgnoreCase);
        var selected = machines.Where(requested.Contains).ToArray();
        if (selected.Length != requested.Count)
            throw new UserFriendlyException("机台名单含有未登记的机台，请刷新后重选");

        var assignment = string.Join(",", selected.Select(name => name + range.Suffix));
        foreach (var cell in cells.Where(cell => cell.GetMachineShelfAssignment() != assignment))
        {
            cell.SetMachineShelfAssignment(assignment);
            await _cellRepository.UpdateAsync(cell);
        }

        return ToDto(range, cells, machines);
    }

    /// <summary>读取实际货架库位，按数据库库区展示，保留同编码的不同记录。</summary>
    public async Task<List<MachineShelfCellDto>> GetShelfCellsAsync()
    {
        var columns = GetShelfColumns(Ranges);
        var cells = await _cellRepository.GetListAsync(cell => cell.CellCode.Length == 14 &&
            columns.Contains(cell.CellCode.Substring(0, 6)));
        return cells.Where(cell => HasShelfCodeFormat(cell.CellCode))
            .OrderBy(cell => cell.WarehouseAreaId)
            .ThenBy(cell => cell.CellCode)
            .ThenBy(cell => cell.ShelfName)
            .ThenBy(cell => cell.Id)
            .Select(ToCellDto)
            .ToList();
    }

    /// <summary>新增一条货架库位记录，允许不同机台共用同一物理点位。</summary>
    [Authorize(WmsPermissions.AddPermission)]
    [UnitOfWork(true, IsolationLevel.Serializable)]
    public async Task<MachineShelfCellDto> CreateShelfCellAsync(CreateMachineShelfCellInput input)
    {
        var code = input.CellCode.Trim();
        var shelfName = string.IsNullOrWhiteSpace(input.ShelfName) ? null : input.ShelfName.Trim();
        if (!IsConfiguredShelfCode(code, GetShelfColumns(Ranges)))
            throw new UserFriendlyException("库位编码必须是已配置货架范围内的三层点位编码");

        var existing = await _cellRepository.GetListAsync(cell => cell.CellCode == code);
        if (existing.Any(cell => cell.RunStatus is CellRunStatus.Selected or CellRunStatus.Run))
            throw new UserFriendlyException("库位正在使用，请在任务结束后新增货架绑定");

        var warehouses = await _warehouseRepository.GetAllWarehousesAsync();
        var candidates = warehouses.Where(warehouse =>
            (input.WarehouseAreaId == null || warehouse.GetAreaByAreaId(input.WarehouseAreaId.Value) != null) &&
            (existing.Count == 0 || existing.Any(cell => cell.WarehouseId == warehouse.Id)))
            .ToList();
        if (candidates.Count == 0)
            throw new UserFriendlyException("找不到与该库位和库区对应的仓库");
        if (candidates.Count > 1)
            throw new UserFriendlyException("无法确定所属仓库，请填写库区编号");

        var warehouseId = candidates[0].Id;
        if (existing.Any(cell => cell.WarehouseId == warehouseId && cell.WarehouseAreaId == input.WarehouseAreaId &&
            string.Equals(cell.ShelfName?.Trim() ?? "", shelfName ?? "", StringComparison.OrdinalIgnoreCase)))
            throw new UserFriendlyException("该库区已存在相同库位编码和货架名称的记录");

        var physicalCell = existing.Where(cell => cell.WarehouseId == warehouseId)
            .OrderByDescending(cell => cell.CellStatus)
            .ThenByDescending(cell => cell.RunStatus == CellRunStatus.Enable)
            .FirstOrDefault();
        var newCell = Cell.CreateMachineShelfCell(GuidGenerator.Create(), warehouseId,
            input.WarehouseAreaId, shelfName, code, input.CellName.Trim(), physicalCell);
        await _cellRepository.InsertAsync(newCell, autoSave: true);
        return ToCellDto(newCell);
    }

    /// <summary>更新一行库位资料，库区和点位必须有效，运行中的库位禁止修改。</summary>
    [UnitOfWork(true)]
    public async Task<MachineShelfCellDto> UpdateShelfCellAsync(Guid id, SetMachineShelfCellInput input)
    {
        var cell = await _cellRepository.FindAsync(id);
        var columns = GetShelfColumns(Ranges);
        if (cell == null || !IsConfiguredShelfCode(cell.CellCode, columns))
            throw new UserFriendlyException("未找到可编辑的机台货架库位");
        if (GetCellRevision(cell) != input.Revision)
            throw new UserFriendlyException("库位资料已更新，请刷新后重新编辑");

        var code = input.CellCode.Trim();
        var name = input.CellName.Trim();
        var shelfName = string.IsNullOrWhiteSpace(input.ShelfName) ? null : input.ShelfName.Trim();
        if (!IsConfiguredShelfCode(code, columns))
            throw new UserFriendlyException("库位编码必须是已配置货架范围内的三层点位编码");
        if (await IsShelfCellInUseAsync(cell.CellCode))
            throw new UserFriendlyException("库位正在使用，请在任务结束后修改");

        if (code != cell.CellCode &&
            await _cellRepository.AnyAsync(item => (item.CellCode == cell.CellCode && item.CellStatus != CellStatus.Nohave) ||
                (item.Id != id && item.CellCode == code)))
            throw new UserFriendlyException("原库位有货或目标编码已存在，无法更改库位编码");

        if (input.WarehouseAreaId != cell.WarehouseAreaId)
        {
            if (input.WarehouseAreaId is int areaId)
            {
                var warehouse = await _warehouseRepository.FindByIdAsync(cell.WarehouseId);
                if (warehouse?.GetAreaByAreaId(areaId) == null)
                    throw new UserFriendlyException($"当前仓库不存在库区 {areaId}");
                cell.BindToWarehouseArea(areaId);
            }
            else
            {
                cell.DisBindFromWarehouseArea();
            }
        }

        cell.RenameShelf(shelfName);
        cell.CellCode = code;
        cell.CellName = name;
        await _cellRepository.UpdateAsync(cell, autoSave: true);
        return ToCellDto(cell);
    }

    /// <summary>仅删除选中的记录，同编码其他库位保留。有货、绑定容器或正在使用时禁止删除。</summary>
    [Authorize(WmsPermissions.DeletePermission)]
    [UnitOfWork(true)]
    public async Task DeleteShelfCellAsync(Guid id, string revision)
    {
        var cell = await _cellRepository.FindByIdAsync(id);
        if (cell == null)
            return;
        if (!IsConfiguredShelfCode(cell.CellCode, GetShelfColumns(Ranges)))
            throw new UserFriendlyException("未找到可删除的机台货架库位");
        if (GetCellRevision(cell) != revision)
            throw new UserFriendlyException("库位资料已更新，请刷新后重新删除");
        if (cell.CellStatus != CellStatus.Nohave || cell.CellBoxes?.Count > 0)
            throw new UserFriendlyException("库位有货或已绑定容器，无法删除");
        if (await IsShelfCellInUseAsync(cell.CellCode))
            throw new UserFriendlyException("库位正在使用，请在任务结束后删除");

        await _cellRepository.DeleteAsync(cell, autoSave: true);
    }

    /// <summary>同编码记录共享物理点位，任一记录被任务占用时该点位不可修改或删除。</summary>
    private Task<bool> IsShelfCellInUseAsync(string cellCode)
    {
        return _cellRepository.AnyAsync(item => item.CellCode == cellCode &&
            (item.RunStatus == CellRunStatus.Selected || item.RunStatus == CellRunStatus.Run));
    }

    /// <summary>返回实际库位字段及并发校验所用的版本。</summary>
    private static MachineShelfCellDto ToCellDto(Cell cell)
    {
        return new MachineShelfCellDto
        {
            Id = cell.Id,
            WarehouseAreaId = cell.WarehouseAreaId,
            ShelfName = cell.ShelfName,
            CellCode = cell.CellCode,
            CellName = cell.CellName,
            Revision = GetCellRevision(cell)
        };
    }

    /// <summary>根据可编辑字段和实体并发标记生成界面提交的版本。</summary>
    private static string GetCellRevision(Cell cell)
    {
        var snapshot = new { cell.Id, cell.WarehouseAreaId, cell.ShelfName, cell.CellCode, cell.CellName, cell.ConcurrencyStamp };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot)));
    }

    /// <summary>读取可写入绑定名单的机台名称，逗号为绑定格式的分隔符。</summary>
    private async Task<string[]> GetMachineNamesAsync()
    {
        var machines = await _machineRepository.GetListAsync();
        return machines.Select(machine => machine.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name) && !name.Contains(','))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>按区域和货架列编号读取候选 CTU 库位，包含同编码的多条记录。</summary>
    private Task<List<Cell>> GetCellsAsync(IReadOnlyList<ShelfRange> ranges)
    {
        var columns = GetShelfColumns(ranges);
        // 使用 List.Contains，避免新编译器将数组的 Contains 绑定到 EF Core 8 无法解析的 Span 重载。
        var areas = ranges.Select(range => (int?)range.AreaId).Distinct().ToList();
        return _cellRepository.GetListAsync(cell => areas.Contains(cell.WarehouseAreaId) &&
            cell.CellType == CellType.CTUCell && cell.CellCode.Length == 14 &&
            columns.Contains(cell.CellCode.Substring(0, 6)));
    }

    /// <summary>展开可维护的物理货架列，使用 List 以便 EF Core 翻译 Contains。</summary>
    private static List<string> GetShelfColumns(IEnumerable<ShelfRange> ranges)
    {
        return ranges.SelectMany(range => Enumerable.Range(range.Start, range.End - range.Start + 1))
            .Select(column => column.ToString()).ToList();
    }

    /// <summary>判断编码是否符合三层货架点位格式，例如 600001X3501013。</summary>
    private static bool HasShelfCodeFormat(string code)
    {
        return code?.Length == 14 && code[6] == 'X' && char.IsAsciiDigit(code[7]) &&
            char.IsAsciiDigit(code[8]) && code[9] == '0' && code[10] is >= '1' and <= '3' &&
            code.EndsWith("013", StringComparison.Ordinal);
    }

    /// <summary>判断编码格式和列号是否属于页面管理的货架范围。</summary>
    private static bool IsConfiguredShelfCode(string code, List<string> columns)
    {
        return HasShelfCodeFormat(code) && columns.Contains(code.Substring(0, 6));
    }

    /// <summary>按区域和物理列筛选有效层位。编码形如 600001X3501013，第十至十一位为层号。</summary>
    private static List<Cell> GetRangeCells(ShelfRange range, IEnumerable<Cell> cells)
    {
        return cells.Where(cell => cell.WarehouseAreaId == range.AreaId &&
            cell.CellCode[6] == 'X' && cell.CellCode[9] == '0' &&
            cell.CellCode[10] is >= '1' and <= '3' && cell.CellCode.EndsWith("013", StringComparison.Ordinal) &&
            int.TryParse(cell.CellCode.AsSpan(0, 6), out var column) && column >= range.Start && column <= range.End)
            .ToList();
    }

    /// <summary>汇总一个范围的当前绑定和物理层位，忽略同层重复记录对层数的影响。</summary>
    private static MachineShelfConfigDto ToDto(ShelfRange range, IReadOnlyList<Cell> cells, string[] machines)
    {
        var names = machines.Where(name => cells.Any(cell => HasAssignment(range, cell, name))).ToArray();
        var layerCount = cells.Select(cell => (Column: cell.CellCode.Substring(0, 6), Layer: cell.CellCode[10]))
            .Distinct().Count();
        var snapshot = cells.OrderBy(cell => cell.Id).Select(cell =>
            new { cell.Id, cell.CellCode, cell.ShelfName, Assignment = cell.GetMachineShelfAssignment() });
        var revision = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot)));
        return new MachineShelfConfigDto
        {
            Id = range.Start,
            End = range.End,
            Kind = range.Kind,
            MachineNames = names,
            ReferenceMachines = range.ReferenceMachines.Split(','),
            MissingLayers = (range.End - range.Start + 1) * 3 - layerCount,
            Revision = revision
        };
    }

    /// <summary>读取已保存绑定，旧库位沿用货架名称和空盒衬方向规则。</summary>
    private static bool HasAssignment(ShelfRange range, Cell cell, string name)
    {
        var assignment = cell.GetMachineShelfAssignment();
        if (assignment != null)
            return assignment.Split(',').Contains(name + range.Suffix, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(cell.ShelfName))
            return false;
        if (range.Kind == "feed")
            return string.Equals(cell.ShelfName, name + range.Suffix, StringComparison.OrdinalIgnoreCase);
        if (range.Kind == "return" && (cell.ShelfName.EndsWith("-1") || cell.ShelfName.EndsWith("-2")))
            return string.Equals(cell.ShelfName, name + range.Suffix, StringComparison.OrdinalIgnoreCase);
        return cell.ShelfName.Contains(name, StringComparison.OrdinalIgnoreCase);
    }
}
