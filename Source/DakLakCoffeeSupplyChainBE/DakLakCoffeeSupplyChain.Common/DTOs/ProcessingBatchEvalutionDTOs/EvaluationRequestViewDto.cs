using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationRequestViewDto
    {
        public Guid RequestId { get; set; }
        public string RequestCode { get; set; } = default!;
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; } = default!;
        public string RequestReason { get; set; } = default!;
        public string? AdditionalNotes { get; set; }
        public DateTime RequestedAt { get; set; }
        public Guid RequestedBy { get; set; }
        public string RequesterName { get; set; } = default!;
        public string Status { get; set; } = default!; // Pending, Approved, Rejected, Completed
        public DateTime? EvaluatedAt { get; set; }
        public Guid? EvaluatedBy { get; set; }
        public string? EvaluatorName { get; set; }
        public string? EvaluationResult { get; set; }
        public string? EvaluationComments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
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
