using TuTa.Wms.Stocks;

namespace TuTa.Wms.Stocks.Dtos
{
    /// <summary>
    /// 查询容器中库存的 DTO
    /// </summary>
    public class StockInBoxDto
    {
        /// <summary>
        /// 产品编号 (物料编码)
        /// </summary>
        public string MaterialCode { get; set; }

        /// <summary>
        /// 产品名称 (物料名称)
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// 产品米数
        /// </summary>
        public decimal ProductLength { get; set; }

        /// <summary>
        /// 纸病名称
        /// </summary>
        public string DefectName { get; set; }

        /// <summary>
        /// 纸病位置
        /// </summary>
        public string DefectPosition { get; set; }

        /// <summary>
        /// 辊号 (生产批号)
        /// </summary>
        public string BatchCode { get; set; }

        /// <summary>
        /// 机号 (供应商批次号)
        /// </summary>
        public string SupplierBatchCode { get; set; }


    }
}