using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuTa.Wms.StockInHistories.ValueObjects;
using Volo.Abp.Domain.Entities.Auditing;

namespace TuTa.Wms.StockInHistories.Aggregates
{
    public class StockInHistory : AuditedAggregateRoot<int>
    {
        private StockInHistory()
        {            
        }

        public StockInHistory(
            string barcode, 
            MaterialInfoOfStockInHistory material, 
            CheckInfoOfStockInHistory checkData,
            SupplierInfoOfStockInHistory supplier,
            StockPlaceOfStockInHistory stockPlace,
            string stockInType,
            decimal inCount,
            DateTime inTime,
            string operatorName)
        {
            Barcode = barcode;
            Material = material;
            CheckData = checkData;
            Supplier = supplier;
            StockPlace = stockPlace;
            StockInType = stockInType;
            InCount = inCount;
            InTime = inTime;
            OperatorName = operatorName;
        }

        /// <summary>
        /// 收料条形码
        /// </summary>
        [StringLength(30)]
        public string Barcode { get; private set; }

        /// <summary>
        /// 物料信息
        /// </summary>
        [Required]
        public MaterialInfoOfStockInHistory Material { get; private set; }

        /// <summary>
        /// 检验信息
        /// </summary>
        [Required]
        public CheckInfoOfStockInHistory CheckData { get; private set; }

        /// <summary>
        /// 供应商信息
        /// </summary>
        [Required]
        public SupplierInfoOfStockInHistory Supplier { get; private set; }

        /// <summary>
        /// 入库存放位置
        /// </summary>
        [Required]
        public StockPlaceOfStockInHistory StockPlace { get; private set; }

        /// <summary>
        /// 入库类型
        /// </summary>
        [StringLength(120)]
        public string StockInType { get; private set; }

        /// <summary>
        /// 入库数量
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal InCount { get; private set; }

        /// <summary>
        /// 入库时间
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime InTime { get; private set; }

        /// <summary>
        /// 操作人员
        /// </summary>
        [StringLength(20)]
        public string OperatorName { get; private set; }
    }
}
