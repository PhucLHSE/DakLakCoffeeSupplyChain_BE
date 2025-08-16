using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs
{
    public class WarehouseInboundRequestViewDto
    {
        public Guid InboundRequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }

        public string FarmerName { get; set; } = string.Empty;
        public string? BusinessStaffName { get; set; }
        public double RequestedQuantity { get; set; }

        public Guid? BatchId { get; set; }
        public string? BatchCode { get; set; }
        
        // Cho cà phê tươi
        public Guid? DetailId { get; set; }
        public string? DetailCode { get; set; }
        public string? CoffeeType { get; set; }
        public string? CropSeasonName { get; set; }
    }
}
