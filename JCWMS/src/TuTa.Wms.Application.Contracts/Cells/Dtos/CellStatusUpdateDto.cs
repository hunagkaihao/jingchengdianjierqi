using System;

namespace TuTa.Wms.Cells.Dtos
{
    public class CellStatusUpdateDto
    {
        /// <summary>
        /// 库位编码
        /// </summary>
        public string CellCode { get; set; }

        /// <summary>
        /// 目标状态
        /// </summary>
        public CellStatus CellStatus { get; set; }
    }
}