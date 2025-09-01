using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria;

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
        
        // ========== ĐÁNH GIÁ CHẤT LƯỢNG DỰA THEO TIÊU CHÍ ==========
        /// <summary>
        /// Danh sách tiêu chí đánh giá chất lượng với actual values và kết quả
        /// </summary>
        public List<QualityCriteriaEvaluationDto>? QualityCriteriaEvaluations { get; set; }
        
        /// <summary>
        /// Ghi chú của expert về quyết định đánh giá
        /// </summary>
        public string? ExpertNotes { get; set; }
    }

    /// <summary>
    /// DTO cho việc đánh giá từng tiêu chí chất lượng
    /// </summary>
    public class QualityCriteriaEvaluationDto
    {
        /// <summary>
        /// ID tiêu chí từ SystemConfiguration
        /// </summary>
        public int CriteriaId { get; set; }
        
        /// <summary>
        /// Tên tiêu chí (VD: PB.MoisturePercent)
        /// </summary>
        public string CriteriaName { get; set; } = string.Empty;
        
        /// <summary>
        /// Mô tả tiêu chí
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Giá trị tối thiểu
        /// </summary>
        public decimal? MinValue { get; set; }
        
        /// <summary>
        /// Giá trị tối đa
        /// </summary>
        public decimal? MaxValue { get; set; }
        
        /// <summary>
        /// Đơn vị
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        
        /// <summary>
        /// Toán tử so sánh (<=, >=, =, <, >)
        /// </summary>
        public string Operator { get; set; } = string.Empty;
        
        /// <summary>
        /// Mức độ nghiêm trọng (Hard, Soft)
        /// </summary>
        public string Severity { get; set; } = string.Empty;
        
        /// <summary>
        /// Nhóm quy tắc
        /// </summary>
        public string RuleGroup { get; set; } = string.Empty;
        
        // ========== GIÁ TRỊ THỰC TẾ VÀ KẾT QUẢ ĐÁNH GIÁ ==========
        
        /// <summary>
        /// Giá trị thực tế đo được
        /// </summary>
        public decimal? ActualValue { get; set; }
        
        /// <summary>
        /// Kết quả đánh giá (PASS/FAIL)
        /// </summary>
        public bool IsPassed { get; set; }
        
        /// <summary>
        /// Lý do không đạt (nếu có)
        /// </summary>
        public string? FailureReason { get; set; }
        
        /// <summary>
        /// Ghi chú bổ sung cho tiêu chí này
        /// </summary>
        public string? Notes { get; set; }
    }
}
