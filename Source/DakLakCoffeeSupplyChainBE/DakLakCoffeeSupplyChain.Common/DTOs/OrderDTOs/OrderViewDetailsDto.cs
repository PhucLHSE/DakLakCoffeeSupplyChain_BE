using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.OrderEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs
{
    public class OrderViewDetailsDto
    {
        // Order base
        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public int? DeliveryRound { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateOnly? ActualDeliveryDate { get; set; }

        public double? TotalAmount { get; set; }

        public string Note { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string CancelReason { get; set; } = string.Empty;

        public Guid DeliveryBatchId { get; set; }

        public string DeliveryBatchCode { get; set; } = string.Empty;

        public string ContractNumber { get; set; } = string.Empty;

        // Order Items
        public List<OrderItemViewDto> OrderItems { get; set; } = new();
    }
}
