using System;
using System.Collections.Generic;
using System.Text;

namespace TuTa.Wms.AgvTasks.Dtos
{
    /// <summary>
    /// 创建CTU任务请求
    /// </summary>
    public class CreateCTUTaskRequest
    {
        /// <summary>
        /// 开始库位编码
        /// </summary>
        public string StartCellCode { get; set; }

        /// <summary>
        /// 结束库位编码
        /// </summary>
        public string EndCellCode { get; set; }
    }
}