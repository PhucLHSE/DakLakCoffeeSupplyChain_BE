using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ShipmentMapper
    {
        // Mapper ShipmentViewAllDto
        public static ShipmentViewAllDto MapToShipmentViewAllDto(this Shipment shipment)
        {
            // Parse DeliveryStatus string to enum
            ShipmentDeliveryStatus status = Enum.TryParse<ShipmentDeliveryStatus>(
                shipment.DeliveryStatus?.Replace(" ", ""), ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ShipmentDeliveryStatus.Pending;

            return new ShipmentViewAllDto
            {
                ShipmentId = shipment.ShipmentId,
                ShipmentCode = shipment.ShipmentCode ?? string.Empty,
                OrderId = shipment.OrderId,
                OrderCode = shipment.Order?.OrderCode ?? string.Empty,
                DeliveryStaffId = shipment.DeliveryStaffId,
                DeliveryStaffName = shipment.DeliveryStaff?.Name ?? string.Empty,
                ShippedQuantity = shipment.ShippedQuantity,
                ShippedAt = shipment.ShippedAt,
                DeliveryStatus = status,
                ReceivedAt = shipment.ReceivedAt,
                CreatedAt = shipment.CreatedAt
            };
        }
    }
}
