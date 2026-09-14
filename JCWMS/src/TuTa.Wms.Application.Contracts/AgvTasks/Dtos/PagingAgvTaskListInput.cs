using System;
using TuTa.Wms.Shared;

namespace TuTa.Wms.AgvTasks.Dtos
{
    public class PagingAgvTaskListInput : PagingBase
    {
        /// <summary>
        /// 关键字
        /// </summary>
        public string Filter { get; set; }
        public DateTime StartCreationTime { get; set; }

        public DateTime EndCreationTime { get; set; }
        /// <summary>
        /// 任务类型
        /// </summary>
        public string AgvTaskStatus { get; set; }
    }
}
