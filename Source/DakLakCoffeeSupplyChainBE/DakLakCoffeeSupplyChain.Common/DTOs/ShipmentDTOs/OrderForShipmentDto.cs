using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs
{
    public class OrderForShipmentDto
    {
        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public double TotalOrderQuantity { get; set; }

        public double TotalDeliveredQuantity { get; set; }

        public double TotalRemainingQuantity { get; set; }

        public string ContractCode { get; set; } = string.Empty;

        public string DeliveryBatchCode { get; set; } = string.Empty;

        public List<OrderItemForShipmentDto> OrderItems { get; set; } = new List<OrderItemForShipmentDto>();
    }

    public class OrderItemForShipmentDto
    {
        public Guid OrderItemId { get; set; }

        public Guid? ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public double OrderQuantity { get; set; }

        public double DeliveredQuantity { get; set; }

        public double RemainingQuantity { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}

