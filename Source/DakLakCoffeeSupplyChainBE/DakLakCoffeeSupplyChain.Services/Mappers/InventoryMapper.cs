using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class InventoryMapper
    {
        public static InventoryListItemDto ToListItemDto(this Inventory inv)
        {
            return new InventoryListItemDto
            {
                InventoryId = inv.InventoryId,
                InventoryCode = inv.InventoryCode,
                WarehouseId = inv.WarehouseId,
                BatchId = inv.BatchId,
                WarehouseName = inv.Warehouse?.Name ?? "N/A",
                BatchCode = inv.Batch?.BatchCode ?? "N/A",
                ProductName = inv.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A",
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "N/A",
                Quantity = inv.Quantity,
                Unit = inv.Unit
            };
        }

        public static InventoryDetailDto ToDetailDto(this Inventory inv)
        {
            return new InventoryDetailDto
            {
                InventoryId = inv.InventoryId,
                InventoryCode = inv.InventoryCode,
                WarehouseId = inv.WarehouseId,
                WarehouseName = inv.Warehouse?.Name ?? "N/A",
                BatchId = inv.BatchId ?? new Guid(),
                BatchCode = inv.Batch?.BatchCode ?? "N/A",
                ProductName = inv.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A",
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "N/A",
                Quantity = inv.Quantity,
                Unit = inv.Unit,
                CreatedAt = inv.CreatedAt,
                UpdatedAt = inv.UpdatedAt
            };
        }
    }
}
