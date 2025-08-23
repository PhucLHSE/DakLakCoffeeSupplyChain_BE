using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    public class EvaluationCreateDto
    {
        [Required(ErrorMessage = "BatchId là bắt buộc")]
        public Guid BatchId { get; set; }
        
        [Required(ErrorMessage = "Kết quả đánh giá là bắt buộc")]
        [StringLength(50, ErrorMessage = "Kết quả đánh giá không được vượt quá 50 ký tự")]
        public string EvaluationResult { get; set; } = default!;
        
        [StringLength(2000, ErrorMessage = "Comments không được vượt quá 2000 ký tự")]
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
        
        /// <summary>
        /// Lý do yêu cầu đánh giá (cho farmer tạo đơn đánh giá)
        /// </summary>
        public string? RequestReason { get; set; }
        
        /// <summary>
        /// Ghi chú bổ sung (cho farmer tạo đơn đánh giá)
        /// </summary>
        public string? AdditionalNotes { get; set; }
    }
}
