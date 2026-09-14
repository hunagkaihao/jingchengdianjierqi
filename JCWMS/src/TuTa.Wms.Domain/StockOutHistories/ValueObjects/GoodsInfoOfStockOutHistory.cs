using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TuTa.Wms.StockOutHistories.ValueObjects
{
    [Owned]
    public class GoodsInfoOfStockOutHistory
    {
        private GoodsInfoOfStockOutHistory()
        {
        }

        public GoodsInfoOfStockOutHistory(string goodsCode, string goodsName, string goodsSpecs)
        {
            Code = goodsCode;
            Name = goodsName;
            Specs = goodsSpecs;
        }


        /// <summary>
        /// 成品编号
        /// </summary>
        [StringLength(30)]
        public string Code { get; private set; }

        /// <summary>
        /// 成品名称
        /// </summary>
        [StringLength(130)]
        public string Name { get; private set; }

        /// <summary>
        /// 成品规格
        /// </summary>
        [StringLength(130)]
        public string Specs { get; private set; }
    }
}
