using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs
{
    public class WalletTransactionCreateDto
    {
        [Required(ErrorMessage = "WalletId là bắt buộc")]
        public Guid WalletId { get; set; }

        public Guid? PaymentId { get; set; }

        [Required(ErrorMessage = "Số tiền là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public double Amount { get; set; }

        [Required(ErrorMessage = "Loại giao dịch là bắt buộc")]
        [StringLength(50, ErrorMessage = "Loại giao dịch không được vượt quá 50 ký tự")]
        public string TransactionType { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }
    }
}
