using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs
{
    public class ShipmentDetailViewDto
    {
        public Guid ShipmentDetailId { get; set; }

        public Guid OrderItemId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public double? Quantity { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductUnit Unit { get; set; }

        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
