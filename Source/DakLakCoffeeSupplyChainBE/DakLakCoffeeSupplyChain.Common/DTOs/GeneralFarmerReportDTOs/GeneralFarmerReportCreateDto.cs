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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReportTypeEnum ReportType { get; set; }

        public Guid? CropProgressId { get; set; }

        public Guid? ProcessingProgressId { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Mức độ nghiêm trọng là bắt buộc.")]
        [Range(0, 2)]
        public SeverityLevel SeverityLevel { get; set; }


        [MaxLength(1000, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 1000 ký tự.")]
        public string? ImageUrl { get; set; }

        [MaxLength(1000, ErrorMessage = "Đường dẫn video không được vượt quá 1000 ký tự.")]
        public string? VideoUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReportType == ReportTypeEnum.Crop && CropProgressId == null)
            {
                yield return new ValidationResult(
                    "CropProgressId là bắt buộc khi loại báo cáo là 'Crop'.",
                    new[] { nameof(CropProgressId) }
                );
            }

            if (ReportType == ReportTypeEnum.Processing && ProcessingProgressId == null)
            {
                yield return new ValidationResult(
                    "ProcessingProgressId là bắt buộc khi loại báo cáo là 'Processing'.",
                    new[] { nameof(ProcessingProgressId) }
                );
            }
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
