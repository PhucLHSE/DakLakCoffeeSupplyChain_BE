using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs
{
    public class InventoryDetailDto
    {
        public Guid InventoryId { get; set; }
        public string InventoryCode { get; set; } = default!;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = default!;
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; } = default!;
        public string ProductName { get; set; } = default!;
        public string CoffeeTypeName { get; set; } = default!;
        public double Quantity { get; set; }
        public string Unit { get; set; } = "kg";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
