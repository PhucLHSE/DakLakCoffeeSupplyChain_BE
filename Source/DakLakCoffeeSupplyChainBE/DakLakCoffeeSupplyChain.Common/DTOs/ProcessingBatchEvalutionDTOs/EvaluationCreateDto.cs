using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationCreateDto
    {
        public Guid BatchId { get; set; }
        public string EvaluationResult { get; set; } = default!;
        public string? Comments { get; set; }
        public DateTime? EvaluatedAt { get; set; }
        
        /// <summary>
        /// Mô tả chi tiết vấn đề theo tiến trình (nếu có)
        /// Format: "Tiến trình 2 (Rang): nhiệt độ quá cao, cần điều chỉnh"
        /// </summary>
        public string? DetailedFeedback { get; set; }
        
        /// <summary>
        /// Danh sách tiến trình có vấn đề (nếu có)
        /// Format: ["Step 2: Roasting", "Step 3: Grinding"]
        /// </summary>
        public List<string>? ProblematicSteps { get; set; }
        
        /// <summary>
        /// Khuyến nghị cải thiện (nếu có)
        /// </summary>
        public string? Recommendations { get; set; }
    }
}
