using DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs
{
    public class ProcurementPlanCreateDto
    {
        [Required(ErrorMessage = "Tên kế hoạch không được để trống.")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mô tả không được để trống.")]
        public string Description { get; set; } = string.Empty;
        [Required(ErrorMessage = "Ngày bắt đầu nhận đăng ký không được để trống.")]
        public DateOnly StartDate { get; set; }
        [Required(ErrorMessage = "Ngày kết thúc nhận đăng ký không được để trống.")]
        public DateOnly EndDate { get; set; }
        [Required(ErrorMessage = "Chưa đăng nhập")]
        public Guid CreatedById { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcurementPlanStatus Status { get; set; } = ProcurementPlanStatus.Draft;

    }
}
