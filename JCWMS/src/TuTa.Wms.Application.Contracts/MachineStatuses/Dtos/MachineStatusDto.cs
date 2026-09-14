namespace TuTa.Wms.MachineStatuses.Dtos
{
    /// <summary>
    /// 机台门状态 DTO（来源：Redis Wms:MachineStatus）
    /// </summary>
    public class MachineStatusDto
    {
        /// <summary>
        /// 机台名称
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// 门号
        /// </summary>
        public int DoorNumber { get; set; }

        /// <summary>
        /// 门名称（如 1号门）
        /// </summary>
        public string DoorName { get; set; }

        /// <summary>
        /// 关联库位编码
        /// </summary>
        public string CellCode { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; }

        /// <summary>
        /// 满料信号地址
        /// </summary>
        public int FullMaterialAddress { get; set; }

        /// <summary>
        /// 是否在线（Modbus 读取成功）
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// 满料信号值（断线时恒为 false）
        /// </summary>
        public bool FullMaterial { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public string LastUpdateTime { get; set; }

        /// <summary>
        /// 进行中任务 ID（无任务为 null）
        /// </summary>
        public int? TaskId { get; set; }

        /// <summary>
        /// 进行中任务状态值（无任务为 null）
        /// </summary>
        public int? TaskStatus { get; set; }

        /// <summary>
        /// 任务状态文本（无任务为 "无任务"，查询异常为 "未知"）
        /// </summary>
        public string TaskStatusText { get; set; }
    }
}
