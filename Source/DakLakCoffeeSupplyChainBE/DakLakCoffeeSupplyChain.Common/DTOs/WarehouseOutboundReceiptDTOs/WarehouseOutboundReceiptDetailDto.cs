using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs
{
    public class WarehouseOutboundReceiptDetailDto
    {
        public Guid OutboundReceiptId { get; set; }
        public string OutboundReceiptCode { get; set; } = default!;
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? BatchId { get; set; }
        public string? BatchCode { get; set; }
        public double Quantity { get; set; }
        public DateTime? ExportedAt { get; set; }
        public string? StaffName { get; set; }
        public string? Note { get; set; }
        public string? DestinationNote { get; set; }
    }
}
