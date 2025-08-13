using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs
{
    public class WalletListDto
    {
        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }
        public string WalletType { get; set; }
        public double TotalBalance { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Thông tin người dùng
        public string? UserName { get; set; }
        public string? UserCode { get; set; }
    }
}
