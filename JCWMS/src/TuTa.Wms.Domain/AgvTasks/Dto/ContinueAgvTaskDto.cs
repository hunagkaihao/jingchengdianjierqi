using System;

namespace TuTa.Wms.AgvTasks.Dto
{
    public class ContinueAgvTaskDto
    {
        public ContinueAgvTaskDto(string reqcode, string taskcode)
        {
            this.reqCode = reqcode;
            this.taskCode = taskcode;
        }
        /// <summary>
        /// 任务请求编号，唯一
        /// </summary>
        public string reqCode { get; set; }
        /// <summary>
        /// 请求时间截 格式: "yyyy-MM-dd HH:mm:ss"。
        /// </summary>
        public string taskCode { get; set; }
    }
}