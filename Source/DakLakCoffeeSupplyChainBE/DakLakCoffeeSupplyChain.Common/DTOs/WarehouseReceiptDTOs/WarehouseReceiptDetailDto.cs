using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs
{
    public class WarehouseReceiptDetailDto
    {
        public Guid ReceiptId { get; set; }
        public string ReceiptCode { get; set; } = default!;
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid BatchId { get; set; }
        public string? BatchCode { get; set; }
        
        // Thông tin cho cà phê tươi
        public Guid? DetailId { get; set; }
        public string? DetailCode { get; set; }
        public string? CoffeeType { get; set; }
        public string? CropSeasonName { get; set; }
        
        public double? ReceivedQuantity { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? StaffName { get; set; }
        public string? Note { get; set; }
        
        // ✅ Thêm số lượng yêu cầu nhập từ inbound request
        public double RequestedQuantity { get; set; }
        
        // ✅ Thêm thông tin số lượng còn lại thực tế
        public double RemainingQuantity { get; set; }
        public double TotalReceivedSoFar { get; set; }
    }
}
