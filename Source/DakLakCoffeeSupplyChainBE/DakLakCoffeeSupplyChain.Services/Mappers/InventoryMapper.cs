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
                BatchCode = inv.Batch?.BatchCode ?? "Không có",
                ProductName = inv.BatchId != null 
                    ? (inv.Batch?.CoffeeType?.TypeName ?? "Cà phê sơ chế") + " (Sơ chế)" + " - Batch: " + (inv.Batch?.BatchCode ?? "N/A")
                    : (inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "Cà phê tươi") + " (Tươi)" + " - Mùa vụ: " + (inv.Detail?.CropSeason?.SeasonName ?? "N/A"),
                CoffeeTypeName = inv.Batch?.CoffeeType?.TypeName ?? "Không xác định",
                
                // Thông tin cho cà phê tươi
                DetailId = inv.DetailId,
                DetailCode = inv.Detail?.CropSeason?.SeasonName ?? "Không có",
                CropSeasonName = inv.Detail?.CropSeason?.SeasonName ?? "Không có",
                CoffeeTypeNameDetail = inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "Không có",
                
                WarehouseName = inv.Warehouse?.Name ?? "Không có",
                Quantity = inv.Quantity,
                Unit = inv.Unit
            };
        }

        public static InventoryDetailDto ToDetailDto(this Inventory inv)
        {
            // Xác định loại cà phê dựa trên BatchId và DetailId
            bool isProcessedCoffee = inv.BatchId.HasValue && inv.BatchId != Guid.Empty;
            bool isFreshCoffee = inv.DetailId.HasValue && inv.DetailId != Guid.Empty;

            return new InventoryDetailDto
            {
                InventoryId = inv.InventoryId,
                InventoryCode = inv.InventoryCode,
                WarehouseId = inv.WarehouseId,
                WarehouseName = inv.Warehouse?.Name ?? "Không có",
                
                // Thông tin cho cà phê sơ chế
                BatchId = inv.BatchId ?? Guid.Empty,
                BatchCode = isProcessedCoffee 
                    ? (inv.Batch?.BatchCode ?? "Không có mã lô")
                    : "Không áp dụng",
                ProductName = isProcessedCoffee
                    ? (inv.Batch?.CoffeeType?.TypeName ?? "Cà phê sơ chế") + " (Sơ chế)" + " - Batch: " + (inv.Batch?.BatchCode ?? "N/A")
                    : (isFreshCoffee 
                        ? (inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "Cà phê tươi") + " (Tươi)" + " - Mùa vụ: " + (inv.Detail?.CropSeason?.SeasonName ?? "N/A")
                        : "Không xác định"),
                CoffeeTypeName = isProcessedCoffee
                    ? (inv.Batch?.CoffeeType?.TypeName ?? "Không xác định loại cà phê")
                    : (isFreshCoffee
                        ? (inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "Cà phê tươi")
                        : "Không xác định"),
                
                // Thông tin cho cà phê tươi
                DetailId = inv.DetailId,
                DetailCode = isFreshCoffee 
                    ? (inv.Detail?.CropSeason?.SeasonName ?? "Không có tên mùa vụ")
                    : "Không áp dụng",
                CropSeasonName = isFreshCoffee 
                    ? (inv.Detail?.CropSeason?.SeasonName ?? "Không có tên mùa vụ")
                    : "Không áp dụng",
                CoffeeTypeNameDetail = isFreshCoffee
                    ? (inv.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "Cà phê tươi")
                    : "Không áp dụng",
                
                Quantity = inv.Quantity,
                Unit = inv.Unit,
                CreatedAt = inv.CreatedAt,
                UpdatedAt = inv.UpdatedAt
            };
        }
    }
}
