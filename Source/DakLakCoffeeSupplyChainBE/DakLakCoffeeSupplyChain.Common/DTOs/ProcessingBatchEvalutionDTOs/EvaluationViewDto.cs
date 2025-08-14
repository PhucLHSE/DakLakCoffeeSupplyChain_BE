using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationViewDto
    {
        public Guid EvaluationId { get; set; }
        public string EvaluationCode { get; set; } = default!;
        public Guid BatchId { get; set; }
        public Guid? EvaluatedBy { get; set; }

        public string EvaluationResult { get; set; } = default!;
        public string? Comments { get; set; }
        public DateTime? EvaluatedAt { get; set; }
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
        
        // Thông tin batch để FE hiển thị
        public string? BatchCode { get; set; }
        public string? FarmerName { get; set; }
        public string? MethodName { get; set; }
        public double? InputQuantity { get; set; }
        public string? InputUnit { get; set; }
        public string? BatchStatus { get; set; }
        
        // Thông tin expert (người đánh giá)
        public string? ExpertName { get; set; }
    }

}
