namespace TuTa.Wms.Stocks.Dtos
{
    public class StockCreateDto
    {
        /// <summary>
        /// 收料条形码
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// 物料总数
        /// </summary>
        public decimal TotalCount { get; set; }
    }
}
