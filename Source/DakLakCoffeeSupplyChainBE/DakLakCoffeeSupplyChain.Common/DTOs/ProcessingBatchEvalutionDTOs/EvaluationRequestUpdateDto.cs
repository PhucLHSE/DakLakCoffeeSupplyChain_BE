using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationRequestUpdateDto
    {
        public string Status { get; set; } = default!; // Approved, Rejected, Completed
        public string? EvaluationResult { get; set; } // Pass, Fail, NeedsImprovement, Temporary
        public string? EvaluationComments { get; set; }
        public DateTime? EvaluatedAt { get; set; }
        
        /// <summary>
        /// Mô tả chi tiết vấn đề theo tiến trình (nếu có)
        /// </summary>
        public string? DetailedFeedback { get; set; }
        
        /// <summary>
        /// Danh sách tiến trình có vấn đề (nếu có)
        /// </summary>
        public List<string>? ProblematicSteps { get; set; }
        
        /// <summary>
        /// Khuyến nghị cải thiện (nếu có)
        /// </summary>
        public string? Recommendations { get; set; }
    }
}
