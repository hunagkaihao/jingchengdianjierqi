using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TuTa.Wms.ChkResultLists;
using Volo.Abp.Domain.Values;

namespace TuTa.Wms.StockOutHistories.ValueObjects
{
    [Owned]
    public class CheckInfoOfStockOutHistory : ValueObject
    {
        private CheckInfoOfStockOutHistory()
        {
        }

        public CheckInfoOfStockOutHistory(
            string checkOrderCode,
            string checkNo,
            DateTime? checkDate,
            string checkResult,
            string checkNoBeforeReCheck,
            EnumCheckType? checkType)
        {           
            CheckOrderCode = checkOrderCode;
            CheckNo = checkNo;
            CheckDate = checkDate;
            CheckResult = checkResult;
            CheckNoBeforeReCheck = checkNoBeforeReCheck;
            CheckType = checkType;
        }

        /// <summary>
        /// 检验单号
        /// </summary>
        [StringLength(30)]
        public virtual string CheckOrderCode { get; private set; }

        /// <summary>
        /// 检验编号
        /// </summary>
        [StringLength(40)]
        public virtual string CheckNo { get; private set; }

        /// <summary>
        /// 检验日期
        /// </summary>
        [Column(TypeName = "date")]
        public virtual DateTime? CheckDate { get; private set; }

        /// <summary>
        /// 检验结论
        /// 1（合格入仓）  
        /// 2（不合格：第一期不合格不进入中间表）  
        /// 3（超筛代用：允许入仓，但需要车间特别注意）
        /// </summary>
        [StringLength(40)]
        public virtual string CheckResult { get; private set; }

        /// <summary>
        /// 超期复检前的检验单号
        /// </summary>
        [StringLength(40)]
        public virtual string CheckNoBeforeReCheck { get; private set; }

        /// <summary>
        /// 检验类型 
        /// 1(进料检验） 
        /// 2(半成品质检)  
        /// 3(无需检物料收料：第二期放在收料中间表中） 
        /// 4(超期复检)   
        /// 10(期初库存  期初ERP库存生成条码，当检验合格处理）
        /// </summary>
        public virtual EnumCheckType? CheckType { get; private set; }


        protected override IEnumerable<object> GetAtomicValues()
        {
            return new object[] { CheckOrderCode, CheckNo, CheckResult };
        }
    }
}
