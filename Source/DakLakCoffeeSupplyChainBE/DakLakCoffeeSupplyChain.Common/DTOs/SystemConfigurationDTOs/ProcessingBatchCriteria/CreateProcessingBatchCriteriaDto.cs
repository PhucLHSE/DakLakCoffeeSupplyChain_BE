using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria
{
    /// <summary>
    /// DTO cho việc tạo tiêu chí đánh giá chất lượng ProcessingBatch
    /// </summary>
    public class CreateProcessingBatchCriteriaDto
    {
        [Required(ErrorMessage = "Tên tiêu chí là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên tiêu chí không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mô tả tiêu chí là bắt buộc")]
        [StringLength(255, ErrorMessage = "Mô tả tiêu chí không được vượt quá 255 ký tự")]
        public string Description { get; set; } = string.Empty;
        
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        
        [Required(ErrorMessage = "Đơn vị đo là bắt buộc")]
        [StringLength(20, ErrorMessage = "Đơn vị đo không được vượt quá 20 ký tự")]
        public string Unit { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Toán tử so sánh là bắt buộc")]
        [StringLength(10, ErrorMessage = "Toán tử so sánh không được vượt quá 10 ký tự")]
        public string Operator { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mức độ nghiêm trọng là bắt buộc")]
        [StringLength(10, ErrorMessage = "Mức độ nghiêm trọng không được vượt quá 10 ký tự")]
        public string Severity { get; set; } = string.Empty; // Hard, Soft
        
        [Required(ErrorMessage = "Nhóm quy tắc là bắt buộc")]
        [StringLength(50, ErrorMessage = "Nhóm quy tắc không được vượt quá 50 ký tự")]
        public string RuleGroup { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime EffectedDateFrom { get; set; } = DateTime.Now;
        public DateTime? EffectedDateTo { get; set; }
    }
}
