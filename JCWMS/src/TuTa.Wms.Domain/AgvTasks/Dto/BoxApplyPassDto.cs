using System;
using System.Collections.Generic;
using System.Text;

namespace TuTa.Wms.AgvTasks.Dto
{
    /// <summary>
    /// 料箱取放请求DTO
    /// </summary>
    public class BoxApplyPassDto
    {
        /// <summary>
        /// 请求编号，每个请求都要一个唯一编号
        /// </summary>
        public string reqCode { get; set; }
        
        /// <summary>
        /// 请求时间戳，格式："yyyy-MM-dd HH:mm:ss"
        /// </summary>
        public string reqTime { get; set; }
        
        /// <summary>
        /// 客户端编码
        /// </summary>
        public string clientCode { get; set; }
        
        /// <summary>
        /// 令牌编码
        /// </summary>
        public string tokenCode { get; set; }
        
        /// <summary>
        /// 任务编码
        /// </summary>
        public string taskCode { get; set; }
        
        /// <summary>
        /// 类型
        /// </summary>
        public string type { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reqCode">请求编号</param>
        /// <param name="taskCode">任务编码</param>
        /// <param name="type">类型</param>
        public BoxApplyPassDto(string reqCode, string taskCode, string type)
        {
            this.reqCode = reqCode;
            this.reqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.clientCode = "";
            this.tokenCode = "";
            this.taskCode = taskCode;
            this.type = type;
        }
    }
}