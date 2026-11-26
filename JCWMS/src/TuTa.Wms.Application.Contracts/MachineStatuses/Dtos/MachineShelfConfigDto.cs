using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TuTa.Wms.MachineStatuses.Dtos;

/// <summary>一个可配置的货架范围及其机台绑定。</summary>
public class MachineShelfConfigDto
{
    /// <summary>货架范围首列的六位编号。</summary>
    public int Id { get; set; }

    /// <summary>货架范围末列的六位编号，单列时与首列相同。</summary>
    public int End { get; set; }

    /// <summary>货架用途，取值为 feed、finished、semiFinished 或 return。</summary>
    public string Kind { get; set; }

    /// <summary>当前已绑定的机台名称，不含上下空盒衬方向后缀。</summary>
    public IReadOnlyList<string> MachineNames { get; set; }

    /// <summary>当前现场点位表提供的机台名称。</summary>
    public IReadOnlyList<string> ReferenceMachines { get; set; }

    /// <summary>数据库中尚未配置的层位数量，大于零时不可保存。</summary>
    public int MissingLayers { get; set; }

    /// <summary>防止覆盖他人修改的配置版本。</summary>
    public string Revision { get; set; }
}

/// <summary>PDA 货架配置页面数据。</summary>
public class MachineShelvesDto
{
    /// <summary>WMS 中已登记的机台名称。</summary>
    public IReadOnlyList<string> MachineNames { get; set; }

    /// <summary>可查看和配置的货架范围。</summary>
    public IReadOnlyList<MachineShelfConfigDto> Shelves { get; set; }
}

/// <summary>保存一个货架范围的机台绑定。</summary>
public class SetMachineShelfInput
{
    /// <summary>完整的机台名单，空数组解除该范围的专用绑定。</summary>
    [Required]
    public string[] MachineNames { get; set; }

    /// <summary>读取货架时返回的配置版本，共 64 个字符。</summary>
    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string Revision { get; set; }
}

/// <summary>PDA 机台货架表中一条实际库位记录及其可编辑字段。</summary>
public class MachineShelfCellDto : SetMachineShelfCellInput
{
    /// <summary>库位记录 ID，同一物理编码存在多条记录时分别编辑。</summary>
    public Guid Id { get; set; }
}

/// <summary>创建一条机台货架库位记录。</summary>
public class CreateMachineShelfCellInput
{
    /// <summary>所属仓库内的库区编号，空值表示未分区。</summary>
    [Range(1, int.MaxValue)]
    public int? WarehouseAreaId { get; set; }

    /// <summary>实际货架名称，空值表示未指定。</summary>
    [StringLength(30)]
    public string ShelfName { get; set; }

    /// <summary>AGV 和货架传感器使用的物理库位编码。</summary>
    [Required, StringLength(20)]
    public string CellCode { get; set; }

    /// <summary>库位显示名称。</summary>
    [Required, StringLength(50)]
    public string CellName { get; set; }
}

/// <summary>更新一条机台货架库位记录，需提交当前版本。</summary>
public class SetMachineShelfCellInput : CreateMachineShelfCellInput
{
    /// <summary>读取时的记录版本，用于防止覆盖已更新的配置。</summary>
    [Required, StringLength(64, MinimumLength = 64)]
    public string Revision { get; set; }
}
