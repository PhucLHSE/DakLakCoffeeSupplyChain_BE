using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs
{
    public class InventoryCreateDto
    {
        public Guid WarehouseId { get; set; }
        public Guid BatchId { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; } = "kg";
    }
}
