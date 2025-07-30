using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs
{
    public class WarehouseInboundRequestFarmerViewDto
    {
        public Guid InboundRequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double RequestedQuantity { get; set; }

        public DateOnly PreferredDeliveryDate { get; set; }
        public string? Note { get; set; }

        public Guid? BatchId { get; set; }
        public string? BatchCode { get; set; }
        public string? CoffeeType { get; set; }
        public string? SeasonCode { get; set; }
    }
}
