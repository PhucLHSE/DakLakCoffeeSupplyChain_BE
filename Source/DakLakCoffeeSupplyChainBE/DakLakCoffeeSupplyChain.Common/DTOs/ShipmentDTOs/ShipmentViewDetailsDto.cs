using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs
{
    public class ShipmentViewDetailsDto
    {
        public Guid ShipmentId { get; set; }

        public string ShipmentCode { get; set; } = string.Empty;

        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public Guid DeliveryStaffId { get; set; }

        public string DeliveryStaffName { get; set; } = string.Empty;

        public double? ShippedQuantity { get; set; }

        public DateTime? ShippedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShipmentDeliveryStatus DeliveryStatus { get; set; } = ShipmentDeliveryStatus.Pending;

        public DateTime? ReceivedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        // ShipmentDetails list
        public List<ShipmentDetailViewDto> ShipmentDetails { get; set; } = new();
    }
}
