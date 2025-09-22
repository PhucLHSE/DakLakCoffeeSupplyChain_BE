namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs
{
    public class WalletTransactionSearchResultDto
    {
        public List<WalletTransactionListDto> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
