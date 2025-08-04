using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs
{
    public class CropProgressUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ProgressId là bắt buộc.")]
        public Guid ProgressId { get; set; }

        [Required(ErrorMessage = "CropSeasonDetailId là bắt buộc.")]
        public Guid CropSeasonDetailId { get; set; }

        [Required(ErrorMessage = "StageId là bắt buộc.")]
        public int StageId { get; set; }

        [Required(ErrorMessage = "Mô tả giai đoạn là bắt buộc.")]
        [MaxLength(500, ErrorMessage = "Mô tả giai đoạn không được vượt quá 500 ký tự.")]
        public string StageDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày thực hiện là bắt buộc.")]
        public DateOnly ProgressDate { get; set; }

        [MaxLength(1000, ErrorMessage = "Link hình ảnh không được vượt quá 1000 ký tự.")]
        public string? PhotoUrl { get; set; }

        [MaxLength(1000, ErrorMessage = "Link video không được vượt quá 1000 ký tự.")]
        public string? VideoUrl { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        public int? StepIndex { get; set; }
        public double? ActualYield { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StageId <= 0)
            {
                yield return new ValidationResult(
                    "StageId phải lớn hơn 0.",
                    new[] { nameof(StageId) }
                );
            }

            if (ProgressDate > DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            {
                yield return new ValidationResult(
                    "Ngày thực hiện không được vượt quá ngày hôm nay.",
                    new[] { nameof(ProgressDate) }
                );
            }

            if (StepIndex.HasValue && StepIndex < 0)
            {
                yield return new ValidationResult(
                    "StepIndex không được nhỏ hơn 0.",
                    new[] { nameof(StepIndex) }
                );
            }
        }
    }
}
