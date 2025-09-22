using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Request DTO cho việc tạo VNPay URL
    /// </summary>
    public class VnPayCreateRequest
    {
        /// <summary>
        /// ID của kế hoạch thu mua
        /// </summary>
        [Required]
        public Guid PlanId { get; set; }

        /// <summary>
        /// URL trả về sau khi thanh toán
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Ngôn ngữ hiển thị (mặc định: vn)
        /// </summary>
        public string? Locale { get; set; } = "vn";
    }
}

