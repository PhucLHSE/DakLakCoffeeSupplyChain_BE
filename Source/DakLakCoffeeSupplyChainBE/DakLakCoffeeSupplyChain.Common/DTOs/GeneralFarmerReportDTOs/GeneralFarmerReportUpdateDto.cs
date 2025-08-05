using DakLakCoffeeSupplyChain.Common.Enum.GeneralReportEnums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs
{
    public class GeneralFarmerReportUpdateDto : IValidatableObject
    {
        [Required]
        public Guid ReportId { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [MaxLength(2000)]
        public string Description { get; set; }

        [Range(0, 2)]
        public int SeverityLevel { get; set; }


        [MaxLength(1000)]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? VideoUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(ImageUrl) && !Uri.IsWellFormedUriString(ImageUrl, UriKind.RelativeOrAbsolute))
                yield return new ValidationResult("Đường dẫn hình ảnh không hợp lệ.", new[] { nameof(ImageUrl) });

            if (!string.IsNullOrEmpty(VideoUrl) && !Uri.IsWellFormedUriString(VideoUrl, UriKind.RelativeOrAbsolute))
                yield return new ValidationResult("Đường dẫn video không hợp lệ.", new[] { nameof(VideoUrl) });
        }
    }
}
