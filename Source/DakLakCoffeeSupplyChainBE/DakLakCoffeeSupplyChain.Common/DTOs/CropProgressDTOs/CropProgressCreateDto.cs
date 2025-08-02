using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressCreateDto
    {
        [Required(ErrorMessage = "Chi tiết mùa vụ là bắt buộc.")]
        public Guid CropSeasonDetailId { get; set; }

        [Required(ErrorMessage = "Giai đoạn là bắt buộc.")]
        public int StageId { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả giai đoạn không được vượt quá 500 ký tự.")]
        public string? StageDescription { get; set; }

        [Required(ErrorMessage = "Ngày ghi nhận là bắt buộc.")]
        public DateTime? ProgressDate { get; set; }


        [Range(0.01, double.MaxValue, ErrorMessage = "Sản lượng thực tế phải lớn hơn 0.")]
        public double? ActualYield { get; set; }

        [Url(ErrorMessage = "Ảnh phải là một đường dẫn hợp lệ.")]
        public string? PhotoUrl { get; set; }

        [Url(ErrorMessage = "Video phải là một đường dẫn hợp lệ.")]
        public string? VideoUrl { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự bước phải là số không âm.")]
        public int? StepIndex { get; set; }
    }
}
