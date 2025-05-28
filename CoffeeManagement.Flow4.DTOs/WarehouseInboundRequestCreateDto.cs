using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeManagement.Flow4.DTOs
{
    public class WarehouseInboundRequestCreateDto
    {
        public Guid BatchId { get; set; }
        public Guid FarmerId { get; set; }
        public float RequestedQuantity { get; set; }
        public DateTime? PreferredDeliveryDate { get; set; }
        public string? Note { get; set; }
    }
    public class WarehouseInboundRequestDto
    {
        public Guid InboundRequestId { get; set; }
        public Guid BatchId { get; set; }
        public Guid FarmerId { get; set; }
        public Guid BusinessManagerId { get; set; }
        public float RequestedQuantity { get; set; }
        public DateTime? PreferredDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    public class WarehouseInboundRequestUpdateStatusDto
    {
        public string Status { get; set; } = "approved"; // or "rejected", "completed"
        public DateTime? ActualDeliveryDate { get; set; }
        public string? Note { get; set; }
    }
}
