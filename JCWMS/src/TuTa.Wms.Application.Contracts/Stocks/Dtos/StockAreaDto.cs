namespace TuTa.Wms.Stocks.Dtos
{
    /// <summary>
    /// 库区库存信息DTO
    /// </summary>
    public class StockAreaDto
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// 组织名称
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// 产品米数
        /// </summary>
        public decimal ProductLength { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 产品编号
        /// </summary>
        public string MaterialCode { get; set; }

        /// <summary>
        /// 位置（库位编号）
        /// </summary>
        public string CellCode { get; set; }
    }
}