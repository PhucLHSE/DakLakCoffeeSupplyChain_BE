using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs
{
    public class WarehouseInboundRequestCreateDto
    {
        public Guid? BatchId { get; set; }
        public double RequestedQuantity { get; set; }
        public DateOnly PreferredDeliveryDate { get; set; }
        public string? Note { get; set; }
        public Guid BusinessStaffId { get; set; }

    }
}
