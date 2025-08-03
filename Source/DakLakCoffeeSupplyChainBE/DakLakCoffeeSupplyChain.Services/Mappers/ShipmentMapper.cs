using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        // Mapper ShipmentViewDetailsDto
        public static ShipmentViewDetailsDto MapToShipmentViewDetailsDto(this Shipment shipment)
        {
            // Parse DeliveryStatus string to enum
            ShipmentDeliveryStatus status = Enum.TryParse<ShipmentDeliveryStatus>(
                shipment.DeliveryStatus?.Replace(" ", ""), ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ShipmentDeliveryStatus.Pending;

            return new ShipmentViewDetailsDto
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
                CreatedAt = shipment.CreatedAt,
                CreatedByName = shipment.CreatedByNavigation?.Name ?? string.Empty,
                ShipmentDetails = shipment.ShipmentDetails?
                    .Where(detail => !detail.IsDeleted)
                    .Select(detail =>
                    {
                        // Parse Unit string to enum
                        ProductUnit unit = Enum.TryParse<ProductUnit>(detail.Unit, ignoreCase: true, out var parsedUnit)
                            ? parsedUnit
                            : ProductUnit.Kg;

                        return new ShipmentDetailViewDto
                        {
                            ShipmentDetailId = detail.ShipmentDetailId,
                            OrderItemId = detail.OrderItemId,
                            ProductName = detail.OrderItem?.Product?.ProductName ?? string.Empty,
                            Quantity = detail.Quantity,
                            Unit = unit,
                            Note = detail.Note ?? string.Empty,
                            CreatedAt = detail.CreatedAt
                        };
                    })
                    .ToList() ?? new()
            };
        }

        // Mapper ShipmentCreateDto -> Shipment
        public static Shipment MapToNewShipment(this ShipmentCreateDto dto, string shipmentCode)
        {
            var shipmentId = Guid.NewGuid();

            var shipment = new Shipment
            {
                ShipmentId = shipmentId,
                ShipmentCode = shipmentCode,
                OrderId = dto.OrderId,
                DeliveryStaffId = dto.DeliveryStaffId,
                ShippedQuantity = dto.ShippedQuantity,
                ShippedAt = dto.ShippedAt,
                DeliveryStatus = dto.DeliveryStatus.ToString(), // enum to string
                ReceivedAt = dto.ReceivedAt,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false,

                ShipmentDetails = dto.ShipmentDetails.Select(detail => new ShipmentDetail
                {
                    ShipmentDetailId = Guid.NewGuid(),
                    ShipmentId = shipmentId,
                    OrderItemId = detail.OrderItemId,
                    Quantity = detail.Quantity ?? 0,
                    Unit = detail.Unit.ToString(), // enum to string
                    Note = detail.Note,
                    CreatedAt = DateHelper.NowVietnamTime(),
                    UpdatedAt = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                }).ToList()
            };

            return shipment;
        }

        // Mapper ShipmentUpdateDto -> Shipment
        public static void MapToUpdatedShipment(this ShipmentUpdateDto dto, Shipment shipment)
        {
            shipment.OrderId = dto.OrderId;
            shipment.DeliveryStaffId = dto.DeliveryStaffId;
            shipment.ShippedQuantity = dto.ShippedQuantity;
            shipment.ShippedAt = dto.ShippedAt;
            shipment.DeliveryStatus = dto.DeliveryStatus.ToString(); // enum -> string
            shipment.ReceivedAt = dto.ReceivedAt;
            shipment.UpdatedAt = DateHelper.NowVietnamTime(); // cập nhật thời gian cuối
        }
    }
}
