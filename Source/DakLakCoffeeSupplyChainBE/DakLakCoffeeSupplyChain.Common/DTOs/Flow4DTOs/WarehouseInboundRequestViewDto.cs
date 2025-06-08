using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs
{
    public class WarehouseInboundRequestViewDto
    {
        public Guid InboundRequestId { get; set; }
        public string InboundRequestCode { get; set; }
        public Guid FarmerId { get; set; }
        public string? FarmerName { get; set; }  // Nếu cần join
        public Guid? BusinessStaffId { get; set; }
        public double? RequestedQuantity { get; set; }
        public DateOnly? PreferredDeliveryDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
