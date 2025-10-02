using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// ✅ Request DTO cho việc tái tạo VNPay URL từ payment pending
    /// </summary>
    public class RecreateWalletTopupRequest
    {
        /// <summary>
        /// Payment ID cần tiếp tục thanh toán
        /// </summary>
        [Required]
        public Guid PaymentId { get; set; }

        /// <summary>
        /// Số tiền (lấy từ payment pending)
        /// </summary>
        [Required]
        [Range(10000, 100000000000, ErrorMessage = "Số tiền nạp phải từ 10,000 đến 10,000,000 VND")]
        public int Amount { get; set; }

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

