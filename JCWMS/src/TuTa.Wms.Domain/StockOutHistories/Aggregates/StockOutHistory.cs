using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuTa.Wms.StockInHistories.ValueObjects;
using TuTa.Wms.StockOutHistories.ValueObjects;
using Volo.Abp.Domain.Entities.Auditing;

namespace TuTa.Wms.StockInHistories.Aggregates
{
    public class StockOutHistory : AuditedAggregateRoot<int>
    {
        private StockOutHistory()
        {            
        }

        public StockOutHistory(
            string barcode, 
            MaterialInfoOfStockOutHistory material, 
            CheckInfoOfStockOutHistory checkData,
            SupplierInfoOfStockOutHistory supplier,
            PickerOfStockOutHistory picker,
            GoodsInfoOfStockOutHistory goods,
            StockPlaceOfStockOutHistory stockPlace,
            string stockOutType,
            string pickBatch,
            string uniqueCode,
            decimal outCount,
            DateTime outTime,
            string operatorName)
        {
            Barcode = barcode;
            Material = material;
            CheckData = checkData;
            Supplier = supplier;
            Picker = picker;
            Goods = goods;
            StockPlace = stockPlace;
            StockOutType = stockOutType;
            PickBatch = pickBatch;
            UniqueCode = uniqueCode;
            OutCount = outCount;
            OutTime = outTime;
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
        public MaterialInfoOfStockOutHistory Material { get; private set; }

        /// <summary>
        /// 检验信息
        /// </summary>
        [Required]
        public CheckInfoOfStockOutHistory CheckData { get; private set; }

        /// <summary>
        /// 供应商信息
        /// </summary>
        [Required]
        public SupplierInfoOfStockOutHistory Supplier { get; private set; }

        /// <summary>
        /// 领料单位
        /// </summary>
        [Required]
        public PickerOfStockOutHistory Picker { get; private set; }

        /// <summary>
        /// 领用成品物料
        /// </summary>
        [Required]
        public GoodsInfoOfStockOutHistory Goods { get; private set; }

        /// <summary>
        /// 入库存放位置
        /// </summary>
        [Required]
        public StockPlaceOfStockOutHistory StockPlace { get; private set; }


        /// <summary>
        /// 出库类型
        /// </summary>
        [StringLength(120)]
        public string StockOutType { get; private set; }

        /// <summary>
        /// 领用生产批号，和领料通知单号一一对应，生产领用及外协领用时存在，无计划领用不存在
        /// </summary>
        [StringLength(30)]
        public string PickBatch { get; private set; }

        /// <summary>
        /// 唯一编号
        /// </summary>
        [StringLength(32)]
        public string UniqueCode { get; private set; }

        /// <summary>
        /// 入库数量
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal OutCount { get; private set; }

        /// <summary>
        /// 入库时间
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime OutTime { get; private set; }

        /// <summary>
        /// 操作人员
        /// </summary>
        [StringLength(20)]
        public string OperatorName { get; private set; }
    }
}
