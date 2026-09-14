using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Values;

namespace TuTa.Wms.StockOutHistories.ValueObjects
{
    [Owned]
    public class SupplierInfoOfStockOutHistory : ValueObject
    {
        private SupplierInfoOfStockOutHistory()
        {            
        }

        public SupplierInfoOfStockOutHistory(string supplierCode, string supplierName)
        {
            Code = supplierCode;
            Name = supplierName;
        }

        /// <summary>
        /// 供应商码
        /// </summary>
        [StringLength(20)]
        public string Code { get; private set; }

        /// <summary>
        /// 供应商名称
        /// </summary>
        [StringLength(120)]
        public string Name { get; private set; }

        protected override IEnumerable<object> GetAtomicValues()
        {
            return [ Code, Name ];
        }
    }
}
