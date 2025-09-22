using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Request DTO cho việc nạp tiền vào ví qua VNPay
    /// </summary>
    public class WalletTopupVnPayRequest
    {
        /// <summary>
        /// ID của ví
        /// </summary>
        [Required]
        public Guid WalletId { get; set; }

        /// <summary>
        /// Số tiền nạp (VND)
        /// </summary>
        [Required]
        [Range(10000, 10000000, ErrorMessage = "Số tiền nạp phải từ 10,000 đến 10,000,000 VND")]
        public int Amount { get; set; } = 100000;

        /// <summary>
        /// URL trả về sau khi thanh toán
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Ngôn ngữ hiển thị (mặc định: vn)
        /// </summary>
        public string? Locale { get; set; } = "vn";

        /// <summary>
        /// Mô tả giao dịch
        /// </summary>
        public string? Description { get; set; }
    }
}

