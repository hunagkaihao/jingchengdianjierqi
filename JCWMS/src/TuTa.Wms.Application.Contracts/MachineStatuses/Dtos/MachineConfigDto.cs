namespace TuTa.Wms.MachineStatuses.Dtos
{
    /// <summary>
    /// 机台配置 DTO（用于移动端机台启用/停用配置）
    /// </summary>
    public class MachineConfigDto
    {
        /// <summary>
        /// 机台ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 机台名称
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 机台编号
        /// </summary>
        public int MachineNumber { get; set; }

        /// <summary>
        /// 是否启用自动搬运
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
