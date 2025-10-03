using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs
{
    public class CropRejectDto
    {
        [Required(ErrorMessage = "Lý do từ chối là bắt buộc")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Lý do từ chối phải từ 10-1000 ký tự")]
        public string RejectReason { get; set; } = string.Empty;
    }
}
