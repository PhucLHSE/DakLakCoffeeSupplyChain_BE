using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs
{
    public class WarehouseOutboundRequestCreateDto
    {
        public Guid WarehouseId { get; set; }
        public Guid InventoryId { get; set; }
        public double RequestedQuantity { get; set; }
        public string Unit { get; set; } = "kg";
        public string Purpose { get; set; }
        public string Reason { get; set; }
        public Guid? OrderItemId { get; set; }
    }

}
