using DakLakCoffeeSupplyChain.Common.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs
{
    public class CultivationRegistrationDetailsCreateViewDto : IValidatableObject
    {
        [Required(ErrorMessage = "Kế hoạch chi tiết chưa được chọn.")]
        public Guid PlanDetailId { get; set; }
        public double? EstimatedYield { get; set; }

        [Required(ErrorMessage = "Mức giá mong muốn không được để trống")]
        public double WantedPrice { get; set; }
        public DateOnly? ExpectedHarvestStart { get; set; }
        public DateOnly? ExpectedHarvestEnd { get; set; }
        //public CultivationRegistrationStatus Status { get; set; } = CultivationRegistrationStatus.Pending;
        public string Note { get; set; } = string.Empty;
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Nếu có điền ngày bắt đầu thì không được chọn trong quá khứ
            if (ExpectedHarvestStart.HasValue && ExpectedHarvestStart.Value < DateHelper.ParseDateOnlyFormatVietNamCurrentTime())
                yield return new ValidationResult(
                        "Ngày bắt đầu thu hoạch dự kiến không được ở trong quá khứ",
                        [nameof(ExpectedHarvestStart)]);

            // Nếu có điền ngày kết thúc thì không được chọn trong quá khứ
            if (ExpectedHarvestEnd.HasValue && ExpectedHarvestEnd.Value < DateHelper.ParseDateOnlyFormatVietNamCurrentTime())
                yield return new ValidationResult(
                        "Ngày kết thúc thu hoạch dự kiến không được ở trong quá khứ",
                        [nameof(ExpectedHarvestEnd)]);

            // Nếu có điền cả 2 ngày thì ngày bắt đầu không được phép xuất phát sau ngày kết thúc
            if (ExpectedHarvestEnd.HasValue && ExpectedHarvestStart.HasValue && ExpectedHarvestStart > ExpectedHarvestEnd)
                yield return new ValidationResult(
                        "Ngày bắt đầu thu hoạch dự kiến không được sau ngày kết thúc thu hoạch dự kiến",
                        [nameof(ExpectedHarvestEnd)]);

            // Nếu có điền Ngày bắt đầu thì không được phép để trống ngày kết thúc
            if (!ExpectedHarvestEnd.HasValue && ExpectedHarvestStart.HasValue)
                yield return new ValidationResult(
                        "Ngày kết thúc thu hoạch dự kiến không được để trống nếu bạn đã điền ngày bắt đầu kế hoạch dự kiến",
                        [nameof(ExpectedHarvestEnd)]);

            // Nếu có điền ngày kết thúc thì không được phép để trống ngày bắt đầu            
            if (ExpectedHarvestEnd.HasValue && !ExpectedHarvestStart.HasValue)
                yield return new ValidationResult(
                        "Ngày bắt đầu thu hoạch dự kiến không được để trống nếu bạn đã điền ngày kết thúc kế hoạch dự kiến",
                        [nameof(ExpectedHarvestStart)]);
            
        }
    }
}
