using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Request DTO cho MockIpn (chỉ dùng trong development)
    /// </summary>
    public class MockIpnRequest
    {
        /// <summary>
        /// ID của kế hoạch thu mua
        /// </summary>
        [Required]
        public Guid PlanId { get; set; }

        /// <summary>
        /// Transaction reference (nếu null sẽ tự động tạo từ PlanId)
        /// </summary>
        public string? TxnRef { get; set; }
    }
}
