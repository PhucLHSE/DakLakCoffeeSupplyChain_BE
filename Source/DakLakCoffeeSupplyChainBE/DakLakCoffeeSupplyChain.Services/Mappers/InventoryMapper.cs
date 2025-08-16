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
                
                // Thông tin cho cà phê sơ chế
                BatchId = inv.BatchId,
                BatchCode = inv.Batch?.BatchCode ?? "N/A",
                ProductName = inv.BatchId != null 
                    ? (inv.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A")
                    : (inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "N/A"),
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "N/A",
                
                // Thông tin cho cà phê tươi
                DetailId = inv.DetailId,
                DetailCode = inv.Detail?.CropSeason?.SeasonName ?? "N/A",
                CropSeasonName = inv.Detail?.CropSeason?.SeasonName ?? "N/A",
                CoffeeTypeNameDetail = inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "N/A",
                
                WarehouseName = inv.Warehouse?.Name ?? "N/A",
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
                
                // Thông tin cho cà phê sơ chế
                BatchId = inv.BatchId ?? new Guid(),
                BatchCode = inv.Batch?.BatchCode ?? "N/A",
                ProductName = inv.BatchId != null 
                    ? (inv.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A")
                    : (inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "N/A"),
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "N/A",
                
                // Thông tin cho cà phê tươi
                DetailId = inv.DetailId,
                DetailCode = inv.Detail?.CropSeason?.SeasonName ?? "N/A",
                CropSeasonName = inv.Detail?.CropSeason?.SeasonName ?? "N/A",
                CoffeeTypeNameDetail = inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "N/A",
                
                Quantity = inv.Quantity,
                Unit = inv.Unit,
                CreatedAt = inv.CreatedAt,
                UpdatedAt = inv.UpdatedAt
            };
        }
    }
}
