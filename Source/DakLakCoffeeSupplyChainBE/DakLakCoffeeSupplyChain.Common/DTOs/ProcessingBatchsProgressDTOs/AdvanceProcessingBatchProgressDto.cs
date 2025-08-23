using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs
{
    public class AdvanceProcessingBatchProgressDto
    {
        public DateTime ProgressDate { get; set; }
        public double? OutputQuantity { get; set; }
        public string OutputUnit { get; set; } = null!;
        public string? PhotoUrl { get; set; }
        public string? VideoUrl { get; set; }
        public List<ProcessingParameterInProgressDto>? Parameters { get; set; }
        
        // Stage selection fields - Sửa từ string thành int để nhất quán với model database
        public int? StageId { get; set; }        // ✅ Nhất quán với model: int StageId
        public int? CurrentStageId { get; set; } // ✅ Nhất quán với model: int StageId
        public string? StageDescription { get; set; }
    }
}
