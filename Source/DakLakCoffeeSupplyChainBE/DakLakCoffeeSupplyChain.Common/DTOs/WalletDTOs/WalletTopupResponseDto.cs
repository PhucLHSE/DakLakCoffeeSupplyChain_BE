namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs
{
    public class WalletTopupResponseDto
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
