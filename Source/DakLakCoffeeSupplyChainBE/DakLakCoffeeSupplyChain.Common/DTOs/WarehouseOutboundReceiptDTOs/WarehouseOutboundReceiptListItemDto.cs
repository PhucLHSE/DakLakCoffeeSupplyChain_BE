using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs
{
    public class WarehouseOutboundReceiptListItemDto
    {
        public Guid OutboundReceiptId { get; set; }
        public string OutboundReceiptCode { get; set; } = default!;
        public string? WarehouseName { get; set; }
        public string? BatchCode { get; set; }
        public double Quantity { get; set; }
        public DateTime? ExportedAt { get; set; }
        public string? StaffName { get; set; }
    }
}
