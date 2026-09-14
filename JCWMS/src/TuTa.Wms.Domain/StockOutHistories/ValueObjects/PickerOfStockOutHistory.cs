using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Values;

namespace TuTa.Wms.StockOutHistories.ValueObjects
{
    [Owned]
    public class PickerOfStockOutHistory : ValueObject
    {
        private PickerOfStockOutHistory()
        {
        }

        public PickerOfStockOutHistory(string deptCode, string deptName, string gysCode, string gysName)
        {
            //if ((!string.IsNullOrWhiteSpace(deptCode) ||
            //    !string.IsNullOrWhiteSpace(deptName)) &&
            //    (!string.IsNullOrWhiteSpace(gysCode) ||
            //    !string.IsNullOrWhiteSpace(gysName)))
            //    throw new Exception("领用部门和委外单位不能同时存在");

            DeptCode = deptCode;
            DeptName = deptName;
            GysCode = gysCode;
            GysName = gysName;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            return new object[] { DeptCode, DeptName, GysCode, GysName };
        }

        /// <summary>
        /// 领用部门编号
        /// </summary>
        [StringLength(30)]
        public string DeptCode { get; private set; }

        /// <summary>
        /// 领用部门名称
        /// </summary>
        [StringLength(60)]
        public string DeptName { get; private set; }

        /// <summary>
        /// 领用外协单位编号
        /// </summary>
        [StringLength(30)]
        public string GysCode { get; private set; }

        /// <summary>
        /// 领用外协单位名称
        /// </summary>
        [StringLength(80)]
        public string GysName { get; private set; }
    }
}
