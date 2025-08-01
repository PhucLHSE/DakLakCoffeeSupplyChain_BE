using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs
{
    public class CultivationRegistrationCreateViewDto : IValidatableObject
    {
        [Required(ErrorMessage = "Kế hoạch chưa được chọn.")]
        public Guid PlanId { get; set; }
        [Required(ErrorMessage = "Khu vực đăng ký không được để trống")]
        public double? RegisteredArea { get; set; }
        //[Required(ErrorMessage = "Mức giá mong muốn không được để trống.")]

        //public CultivationRegistrationStatus Status { get; set; } = CultivationRegistrationStatus.Pending;

        public string Note { get; set; } = string.Empty;

        public ICollection<CultivationRegistrationDetailsCreateViewDto> CultivationRegistrationDetailsCreateViewDto { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra có tồn tại detail không
            if (CultivationRegistrationDetailsCreateViewDto == null || CultivationRegistrationDetailsCreateViewDto.Count == 0)
                yield return new ValidationResult(
                        "Phải có ít nhất một bảng chi tiết của đơn đăng ký thu mua",
                        [nameof(CultivationRegistrationDetailsCreateViewDto)]);
        }
    }
}
