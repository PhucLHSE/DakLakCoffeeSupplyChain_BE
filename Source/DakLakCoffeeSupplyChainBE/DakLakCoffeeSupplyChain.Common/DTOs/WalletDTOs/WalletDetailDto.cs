using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs
{
    public class WalletDetailDto
    {
        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }
        public string WalletType { get; set; }
        public double TotalBalance { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsDeleted { get; set; }
        
        // Thông tin người dùng
        public string? UserName { get; set; }
        public string? UserCode { get; set; }
        
        // Thống kê giao dịch
        public int TotalTransactions { get; set; }
        public double TotalInflow { get; set; }
        public double TotalOutflow { get; set; }
    }
}
