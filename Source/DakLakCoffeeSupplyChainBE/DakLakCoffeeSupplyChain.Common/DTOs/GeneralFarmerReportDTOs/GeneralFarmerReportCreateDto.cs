using DakLakCoffeeSupplyChain.Common.Enum.GeneralReportEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs
{
    public class GeneralFarmerReportCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Loại báo cáo là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Loại báo cáo không được vượt quá 100 ký tự.")]
        public string ReportType { get; set; }

        public Guid? CropProgressId { get; set; }

        public Guid? ProcessingProgressId { get; set; }

        [Required(ErrorMessage = "Người báo cáo là bắt buộc.")]
        public Guid ReportedBy { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
        public string Description { get; set; }

        [Range(1, 5, ErrorMessage = "Mức độ nghiêm trọng phải từ 1 đến 5.")]

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SeverityLevel SeverityLevel { get; set; }

        [MaxLength(1000, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 1000 ký tự.")]
        public string? ImageUrl { get; set; }

        [MaxLength(1000, ErrorMessage = "Đường dẫn video không được vượt quá 1000 ký tự.")]
        public string? VideoUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(ImageUrl) && !Uri.IsWellFormedUriString(ImageUrl, UriKind.RelativeOrAbsolute))
            {
                yield return new ValidationResult(
                    "Đường dẫn hình ảnh không hợp lệ.",
                    new[] { nameof(ImageUrl) }
                );
            }

            if (!string.IsNullOrEmpty(VideoUrl) && !Uri.IsWellFormedUriString(VideoUrl, UriKind.RelativeOrAbsolute))
            {
                yield return new ValidationResult(
                    "Đường dẫn video không hợp lệ.",
                    new[] { nameof(VideoUrl) }
                );
            }
        }
    }
}
