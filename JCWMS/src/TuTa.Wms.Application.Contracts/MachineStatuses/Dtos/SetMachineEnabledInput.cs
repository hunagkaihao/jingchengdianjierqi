namespace TuTa.Wms.MachineStatuses.Dtos
{
    /// <summary>
    /// 设置机台启用状态输入
    /// </summary>
    public class SetMachineEnabledInput
    {
        /// <summary>
        /// 机台ID
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
