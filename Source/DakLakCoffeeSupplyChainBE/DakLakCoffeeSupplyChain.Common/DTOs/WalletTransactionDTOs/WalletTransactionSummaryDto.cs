namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs
{
    public class WalletTransactionSummaryDto
    {
        public int TotalTransactions { get; set; }
        public double TotalTopUp { get; set; }
        public double TotalWithdraw { get; set; }
        public double TotalTransfer { get; set; }
        public double TotalPayment { get; set; }
        public DateTime? LastTransaction { get; set; }
        public double CurrentBalance { get; set; }
        public double TotalInflow { get; set; }
        public double TotalOutflow { get; set; }
    }
}
