using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs
{
    public class ShipmentDetailViewDto
    {
        public Guid ShipmentDetailId { get; set; }

        public Guid OrderItemId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public double? Quantity { get; set; }

        public string Unit { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
