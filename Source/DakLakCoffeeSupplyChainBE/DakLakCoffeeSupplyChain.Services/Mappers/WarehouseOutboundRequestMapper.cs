using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
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
                InventoryName = entity.Inventory?.Products?.FirstOrDefault()?.ProductName,
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
    }
}