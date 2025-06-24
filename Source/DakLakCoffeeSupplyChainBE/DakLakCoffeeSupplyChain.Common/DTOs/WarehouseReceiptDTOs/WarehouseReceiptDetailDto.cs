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
        public double ReceivedQuantity { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? StaffName { get; set; }
        public string? Note { get; set; }
    }
}
