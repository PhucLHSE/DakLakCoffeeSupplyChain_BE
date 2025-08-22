// Services.Mappers/WarehouseOutboundRequestMapper.cs
using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseOutboundRequestMapper
    {
        public static WarehouseOutboundRequestListItemDto ToListItemDto(this WarehouseOutboundRequest e)
            => new()
            {
                OutboundRequestId = e.OutboundRequestId,
                OutboundRequestCode = e.OutboundRequestCode,
                Status = e.Status,
                WarehouseId = e.WarehouseId,
                WarehouseName = e.Warehouse?.Name,
                InventoryId = e.InventoryId,
                ProductName = e.Inventory?.Products?.FirstOrDefault()?.ProductName,
                RequestedQuantity = e.RequestedQuantity,
                Unit = e.Unit,
                CreatedAt = e.CreatedAt
            };

        public static WarehouseOutboundRequestDetailDto ToDetailDto(this WarehouseOutboundRequest e)
            => new()
            {
                OutboundRequestId = e.OutboundRequestId,
                OutboundRequestCode = e.OutboundRequestCode,
                WarehouseId = e.WarehouseId,
                WarehouseName = e.Warehouse?.Name,
                InventoryId = e.InventoryId,
                InventoryName = e.Inventory?.Products?.FirstOrDefault()?.ProductName,
                RequestedQuantity = e.RequestedQuantity,
                Unit = e.Unit,
                Purpose = e.Purpose,
                Reason = e.Reason,
                OrderItemId = e.OrderItemId,
                RequestedBy = e.RequestedBy,
                RequestedByName = e.RequestedByNavigation?.CompanyName,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };

        public static WarehouseOutboundRequest ToEntityCreate(
            this WarehouseOutboundRequestCreateDto dto,
            Guid id,
            string code,
            Guid managerId)
            => new()
            {
                OutboundRequestId = id,
                OutboundRequestCode = code,
                WarehouseId = dto.WarehouseId,
                InventoryId = dto.InventoryId,
                RequestedQuantity = dto.RequestedQuantity,
                Unit = dto.Unit,
                Purpose = dto.Purpose ?? "",
                Reason = dto.Reason ?? "",
                OrderItemId = dto.OrderItemId,
                RequestedBy = managerId,
                Status = WarehouseOutboundRequestStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
    }
}
