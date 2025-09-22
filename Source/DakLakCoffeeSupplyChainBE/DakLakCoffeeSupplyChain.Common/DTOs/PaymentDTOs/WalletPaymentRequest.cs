using System;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Request DTO cho thanh toán qua ví nội bộ
    /// </summary>
    public class WalletPaymentRequest
    {
        /// <summary>
        /// ID của kế hoạch thu mua
        /// </summary>
        [Required(ErrorMessage = "PlanId là bắt buộc")]
        public string PlanId { get; set; } = string.Empty;

        /// <summary>
        /// Số tiền thanh toán
        /// </summary>
        [Required(ErrorMessage = "Amount là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount phải lớn hơn 0")]
        public double Amount { get; set; }

        /// <summary>
        /// Mô tả giao dịch
        /// </summary>
        [StringLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự")]
        public string? Description { get; set; }
    }
}
