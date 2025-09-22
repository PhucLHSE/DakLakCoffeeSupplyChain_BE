namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs
{
    public class WalletTransactionDetailDto
    {
        public Guid TransactionId { get; set; }
        public Guid WalletId { get; set; }
        public Guid? PaymentId { get; set; }
        public double Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        
        // Thông tin bổ sung
        public string? WalletType { get; set; }
        public string? UserName { get; set; }
        public string? UserCode { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
