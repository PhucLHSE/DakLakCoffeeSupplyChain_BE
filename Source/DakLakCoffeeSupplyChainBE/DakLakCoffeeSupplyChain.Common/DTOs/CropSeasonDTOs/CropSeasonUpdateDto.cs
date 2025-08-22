using System;
using System.ComponentModel.DataAnnotations;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ID mùa vụ không được để trống")]
        public Guid CropSeasonId { get; set; }
        
        [Required(ErrorMessage = "Tên mùa vụ không được để trống")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên mùa vụ phải từ 3-100 ký tự")]
        public string SeasonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateOnly EndDate { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate >= EndDate)
            {
                yield return new ValidationResult(
                    "Ngày bắt đầu phải trước ngày kết thúc.",
                    new[] { nameof(StartDate), nameof(EndDate) });
            }
        }
    }
}
