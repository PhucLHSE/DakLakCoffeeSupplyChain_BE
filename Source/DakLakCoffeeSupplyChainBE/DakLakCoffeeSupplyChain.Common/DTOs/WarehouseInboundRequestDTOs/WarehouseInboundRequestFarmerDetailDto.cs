using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs
{
    public class WarehouseInboundRequestFarmerDetailDto
    {
        public Guid InboundRequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateOnly PreferredDeliveryDate { get; set; }
        public DateOnly? ActualDeliveryDate { get; set; }

        public double RequestedQuantity { get; set; }
        public string? Note { get; set; }

        public Guid? BatchId { get; set; }
        public string BatchCode { get; set; } = "N/A";
        public string CoffeeType { get; set; } = "N/A";
        public string SeasonCode { get; set; } = "N/A";
        
        // Cho cà phê tươi
        public Guid? DetailId { get; set; }
        public string DetailCode { get; set; } = "N/A";
        public string CropSeasonName { get; set; } = "N/A";
        public string TypeName { get; set; } = "N/A";
    }
}
