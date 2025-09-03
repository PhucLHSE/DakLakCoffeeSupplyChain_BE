using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs
{
    public class WarehouseOutboundRequestDetailDto
    {
        public Guid OutboundRequestId { get; set; }
        public string OutboundRequestCode { get; set; }

        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public Guid InventoryId { get; set; }
        public string? InventoryName { get; set; }

        // ✅ Chỉ xử lý cà phê sơ chế (Batch) - không còn cà phê tươi
        public Guid? BatchId { get; set; }
        public string? BatchCode { get; set; }
        public string? CoffeeTypeName { get; set; }

        public double RequestedQuantity { get; set; }
        public string Unit { get; set; }
        public string Purpose { get; set; }
        public string Reason { get; set; }

        public Guid? OrderItemId { get; set; }

        public Guid RequestedBy { get; set; }
        public string? RequestedByName { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
