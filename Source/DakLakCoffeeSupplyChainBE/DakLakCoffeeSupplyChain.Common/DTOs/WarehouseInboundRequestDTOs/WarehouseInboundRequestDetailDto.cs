using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs
{
    public class WarehouseInboundRequestDetailDto
    {
        public Guid InboundRequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public DateOnly PreferredDeliveryDate { get; set; }
        public DateOnly? ActualDeliveryDate { get; set; }

        public string? Note { get; set; }
        public double RequestedQuantity { get; set; }

        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;
        public string FarmerPhone { get; set; } = string.Empty;

        public Guid? BusinessStaffId { get; set; }
        public string? BusinessStaffName { get; set; }

        public Guid? BatchId { get; set; }
        public string? BatchCode { get; set; }
        public string? CoffeeType { get; set; }
        public string? SeasonCode { get; set; }
    }
}
