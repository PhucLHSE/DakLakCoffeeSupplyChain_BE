using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ShipmentDetailMapper
    {
        // View Mapper: ShipmentDetail -> ShipmentDetailViewDto
        public static ShipmentDetailViewDto MapToShipmentDetailViewDto(this ShipmentDetail shipmentDetail)
        {
            // Parse Unit string to enum
            ProductUnit unit = Enum.TryParse<ProductUnit>(shipmentDetail.Unit, ignoreCase: true, out var parsedUnit)
                ? parsedUnit
                : ProductUnit.Kg;

            return new ShipmentDetailViewDto
            {
                ShipmentDetailId = shipmentDetail.ShipmentDetailId,
                OrderItemId = shipmentDetail.OrderItemId,
                ProductName = shipmentDetail.OrderItem?.Product?.ProductName ?? string.Empty,
                Quantity = shipmentDetail.Quantity,
                Unit = unit,
                Note = shipmentDetail.Note ?? string.Empty,
                CreatedAt = shipmentDetail.CreatedAt
            };
        }

        // Create Mapper: ShipmentDetailCreateDto -> ShipmentDetail
        public static ShipmentDetail MapToNewShipmentDetail(
            this ShipmentDetailCreateDto dto,
            string? fallbackUnitFromProduct = null)
        {
            // Gán fallback nếu cần
            ProductUnit unit = dto.Unit;

            if (!Enum.IsDefined(typeof(ProductUnit), dto.Unit) &&
                !string.IsNullOrWhiteSpace(fallbackUnitFromProduct) &&
                Enum.TryParse<ProductUnit>(fallbackUnitFromProduct, ignoreCase: true, out var parsedUnit))
            {
                unit = parsedUnit;
            }

            return new ShipmentDetail
            {
                ShipmentDetailId = Guid.NewGuid(),
                ShipmentId = dto.ShipmentId,
                OrderItemId = dto.OrderItemId,
                Quantity = dto.Quantity,
                Unit = unit.ToString(),
                Note = dto.Note ?? string.Empty,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Update Mapper: ShipmentDetailUpdateDto -> ShipmentDetail
        public static void MapToUpdateShipmentDetail(
            this ShipmentDetailUpdateDto dto,
            ShipmentDetail shipmentDetail)
        {
            shipmentDetail.ShipmentId = dto.ShipmentId;
            shipmentDetail.OrderItemId = dto.OrderItemId;
            shipmentDetail.Quantity = dto.Quantity ?? 0;
            shipmentDetail.Unit = dto.Unit.ToString();
            shipmentDetail.Note = dto.Note ?? string.Empty;
            shipmentDetail.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
