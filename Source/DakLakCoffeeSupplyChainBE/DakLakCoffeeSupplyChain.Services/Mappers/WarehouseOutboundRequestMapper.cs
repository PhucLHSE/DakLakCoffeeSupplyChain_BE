using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseOutboundRequestMapper
    {
        public static WarehouseOutboundRequestListItemDto ToListItemDto(this WarehouseOutboundRequest entity)
        {
            return new WarehouseOutboundRequestListItemDto
            {
                OutboundRequestId = entity.OutboundRequestId,
                OutboundRequestCode = entity.OutboundRequestCode,
                Status = entity.Status,
                WarehouseId = entity.WarehouseId,               // ✅ thêm vào
                WarehouseName = entity.Warehouse?.Name,
                InventoryId = entity.InventoryId,               // ✅ thêm vào
                RequestedQuantity = entity.RequestedQuantity,
                Unit = entity.Unit,
                CreatedAt = entity.CreatedAt
            };
        }

        public static WarehouseOutboundRequestDetailDto ToDetailDto(this WarehouseOutboundRequest entity)
        {
            return new WarehouseOutboundRequestDetailDto
            {
                OutboundRequestId = entity.OutboundRequestId,
                OutboundRequestCode = entity.OutboundRequestCode,
                WarehouseId = entity.WarehouseId,
                WarehouseName = entity.Warehouse?.Name,
                InventoryId = entity.InventoryId,
                InventoryName = entity.Inventory?.Products?.FirstOrDefault() != null
    ? entity.Inventory.Products.First().ProductName
    : null,
                RequestedQuantity = entity.RequestedQuantity,
                Unit = entity.Unit,
                Purpose = entity.Purpose,
                Reason = entity.Reason,
                OrderItemId = entity.OrderItemId,
                RequestedBy = entity.RequestedBy,
                RequestedByName = entity.RequestedByNavigation?.CompanyName,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static WarehouseOutboundRequest ToEntityCreate(
    this WarehouseOutboundRequestCreateDto dto,
    Guid generatedId,
    string generatedCode,
    Guid managerId
)
        {
            return new WarehouseOutboundRequest
            {
                OutboundRequestId = generatedId,
                OutboundRequestCode = generatedCode,
                WarehouseId = dto.WarehouseId,
                InventoryId = dto.InventoryId,
                RequestedQuantity = dto.RequestedQuantity,
                Unit = dto.Unit,
                Purpose = dto.Purpose,
                Reason = dto.Reason,
                OrderItemId = dto.OrderItemId,
                RequestedBy = managerId,
                Status = WarehouseOutboundRequestStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

    }
}