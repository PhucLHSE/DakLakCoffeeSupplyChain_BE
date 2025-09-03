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
                ProductName = GetDisplayName(e.Inventory),
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
                
                // ✅ Chỉ xử lý cà phê sơ chế (Batch) - không còn cà phê tươi
                BatchId = e.Inventory?.BatchId,
                BatchCode = e.Inventory?.Batch?.BatchCode,
                CoffeeTypeName = e.Inventory?.Batch?.CoffeeType?.TypeName,
                
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

        private static string GetDisplayName(Inventory inventory)
        {
            if (inventory == null) return "N/A";
            
            // Ưu tiên hiển thị tên sản phẩm nếu có
            var productName = inventory.Products?.FirstOrDefault()?.ProductName;
            if (!string.IsNullOrEmpty(productName)) return productName;
            
            // Nếu có batch, hiển thị tên batch
            if (inventory.Batch != null)
            {
                return $"Mẻ {inventory.Batch.BatchCode}";
            }
            
            // Nếu có crop season detail, hiển thị thông tin mùa vụ
            if (inventory.Detail != null)
            {
                return $"Mùa vụ {inventory.Detail.CropSeason?.SeasonName ?? inventory.Detail.CropSeasonId.ToString()}";
            }
            
            // Fallback
            return "Cà phê";
        }
    }
}
