using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria
{
    /// <summary>
    /// DTO cho việc cập nhật tiêu chí đánh giá chất lượng ProcessingBatch
    /// </summary>
    public class UpdateProcessingBatchCriteriaDto
    {
        [StringLength(255, ErrorMessage = "Mô tả tiêu chí không được vượt quá 255 ký tự")]
        public string? Description { get; set; }
        
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        
        [StringLength(20, ErrorMessage = "Đơn vị đo không được vượt quá 20 ký tự")]
        public string? Unit { get; set; }
        
        [StringLength(10, ErrorMessage = "Toán tử so sánh không được vượt quá 10 ký tự")]
        public string? Operator { get; set; }
        
        [StringLength(10, ErrorMessage = "Mức độ nghiêm trọng không được vượt quá 10 ký tự")]
        public string? Severity { get; set; }
        
        [StringLength(50, ErrorMessage = "Nhóm quy tắc không được vượt quá 50 ký tự")]
        public string? RuleGroup { get; set; }
        
        public bool? IsActive { get; set; }
        public DateTime? EffectedDateFrom { get; set; }
        public DateTime? EffectedDateTo { get; set; }
    }
}
