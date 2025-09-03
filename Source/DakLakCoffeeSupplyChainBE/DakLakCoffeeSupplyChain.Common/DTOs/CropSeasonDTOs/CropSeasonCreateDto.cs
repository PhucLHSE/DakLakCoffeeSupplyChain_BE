using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Cam kết không được để trống")]
        public Guid CommitmentId { get; set; }
        
        [Required(ErrorMessage = "Tên mùa vụ không được để trống")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên mùa vụ phải từ 3-100 ký tự")]
        public string SeasonName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateOnly StartDate { get; set; }
        
        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateOnly EndDate { get; set; }
        
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate >= EndDate)
            {
                yield return new ValidationResult(
                    "Ngày bắt đầu phải trước ngày kết thúc.",
                    new[] { nameof(StartDate), nameof(EndDate) });
            }

            // Validation thời gian mùa vụ phải trong khoảng 11-12 tháng
            var monthsDiff = (EndDate.Year - StartDate.Year) * 12 + 
                           (EndDate.Month - StartDate.Month);
            
            if (monthsDiff < 11 || monthsDiff > 15) // Cho phép sai số 2 tháng để xử lý thiên tai
            {
                yield return new ValidationResult(
                    "Thời gian mùa vụ phải trong khoảng 11-15 tháng (có thể kéo dài thêm 2-3 tháng nếu gặp thiên tai).",
                    new[] { nameof(StartDate), nameof(EndDate) });
            }
        }
    }
}
