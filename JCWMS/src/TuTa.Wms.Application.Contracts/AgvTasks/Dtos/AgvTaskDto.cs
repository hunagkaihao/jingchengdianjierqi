using System;

namespace TuTa.Wms.AgvTasks.Dtos
{
    public class AgvTaskDto
    {
        public int Id { get; set; }
        public string StartPosition { get; set; }
        public string TargetPosition { get; set; }
        public string BoxCode { get; set; }
        public string TaskType { get; set; }
        public int TaskStatus { get; set; }
        public DateTime CreateTime { get; set; }
        public int StockTyp { get; set; }
    }
}
