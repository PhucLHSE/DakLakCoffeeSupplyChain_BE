using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs
{
    public class CropSeasonDetailCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ID mùa vụ không được để trống")]
        public Guid CropSeasonId { get; set; }
        
        [Required(ErrorMessage = "ID chi tiết cam kết không được để trống")]
        public Guid CommitmentDetailId { get; set; }
        
        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Diện tích phân bổ phải lớn hơn 0")]
        public double? AreaAllocated { get; set; }
        
        [StringLength(200, ErrorMessage = "Chất lượng dự kiến không được vượt quá 200 ký tự")]
        public string? PlannedQuality { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CropDetailStatus Status { get; set; }
        
        public Guid? CoffeeTypeId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra thời gian thu hoạch
            if (ExpectedHarvestStart.HasValue && ExpectedHarvestEnd.HasValue)
            {
                if (ExpectedHarvestStart.Value >= ExpectedHarvestEnd.Value)
                {
                    yield return new ValidationResult(
                        "Thời gian bắt đầu thu hoạch phải trước thời gian kết thúc thu hoạch.",
                        new[] { nameof(ExpectedHarvestStart), nameof(ExpectedHarvestEnd) });
                }
            }

            // Kiểm tra năm thu hoạch hợp lệ
            var currentYear = DateOnly.FromDateTime(DateTime.Now).Year;
            if (ExpectedHarvestStart.HasValue && 
                (ExpectedHarvestStart.Value.Year < currentYear - 1 || ExpectedHarvestStart.Value.Year > currentYear + 2))
            {
                yield return new ValidationResult(
                    "Năm thu hoạch phải từ năm trước đến 2 năm sau.",
                    new[] { nameof(ExpectedHarvestStart) });
            }

            if (ExpectedHarvestEnd.HasValue && 
                (ExpectedHarvestEnd.Value.Year < currentYear - 1 || ExpectedHarvestEnd.Value.Year > currentYear + 2))
            {
                yield return new ValidationResult(
                    "Năm thu hoạch phải từ năm trước đến 2 năm sau.",
                    new[] { nameof(ExpectedHarvestEnd) });
            }
        }
    }
}
