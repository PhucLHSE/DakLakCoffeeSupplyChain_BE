using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs.ViewDetailsDtos;
using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs
{
    public class ProcurementPlanCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Tên kế hoạch không được để trống.")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mô tả không được để trống.")]
        public string Description { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        [Required(ErrorMessage = "Ngày kết thúc nhận đăng ký không được để trống.")]
        public DateOnly EndDate { get; set; }
        [Required(ErrorMessage = "Chưa đăng nhập")]
        public Guid CreatedById { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcurementPlanStatus Status { get; set; } = ProcurementPlanStatus.Draft;

        public ICollection<ProcurementPlanDetailsCreateDto> ProcurementPlansDetails { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra có tồn tại detail không
            if (ProcurementPlansDetails == null || ProcurementPlansDetails.Count == 0)
                yield return new ValidationResult(
                        "Phải có ít nhất một chi tiết kế hoạch thu mua",
                        [nameof(ProcurementPlansDetails)]);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            // Nếu có StartDate → không được trong quá khứ
            if (StartDate.HasValue && StartDate.Value < today)
                yield return new ValidationResult(
                        "Ngày bắt đầu không được ở trong quá khứ",
                        [nameof(StartDate)]);

            // EndDate luôn required → check ngày quá khứ
            if (EndDate < today)
                yield return new ValidationResult(
                        "Ngày kết thúc không được ở trong quá khứ",
                        [nameof(EndDate)]);

            // Nếu trạng thái là DRAFT và StartDate có giá trị → so sánh logic
            if (Status == ProcurementPlanStatus.Draft && StartDate.HasValue)
                if (StartDate.Value > EndDate)
                    yield return new ValidationResult(
                            "Ngày bắt đầu không thể sau ngày kết thúc",
                            [nameof(StartDate), nameof(EndDate)]);
        }
    }
}
