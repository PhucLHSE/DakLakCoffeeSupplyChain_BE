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
        public static InventoryListItemDto ToListItemDto(
            this Inventory inv)
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
                // Thêm thông tin vị trí kho
                WarehouseLocation = inv.Warehouse?.Location,
                Quantity = inv.Quantity,
                Unit = inv.Unit,
                
                // Thông tin FIFO (mặc định)
                CreatedAt = inv.CreatedAt,
                FifoPriority = 0,
                IsRecommended = false,
                FifoRecommendation = ""
            };
        }

        public static InventoryListItemDto ToListItemDto(
            this Inventory inv, 
            int fifoPriority = 0,
            bool isRecommended = false, 
            string fifoRecommendation = "")
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
                // Thêm thông tin vị trí kho
                WarehouseLocation = inv.Warehouse?.Location,
                Quantity = inv.Quantity,
                Unit = inv.Unit,
                
                // Thông tin FIFO
                CreatedAt = inv.CreatedAt,
                FifoPriority = fifoPriority,
                IsRecommended = isRecommended,
                FifoRecommendation = fifoRecommendation
            };
        }

        public static InventoryDetailDto ToDetailDto(
            this Inventory inv)
        {
            // Xác định loại cà phê dựa trên BatchId và DetailId
            bool isProcessedCoffee = inv.BatchId.HasValue && inv.BatchId != Guid.Empty;
            bool isFreshCoffee = inv.DetailId.HasValue && inv.DetailId != Guid.Empty;

            // Lấy thông tin từ Farmer và ProcessingBatchEvaluations
            var farmer = inv.Batch?.Farmer;
            var evaluation = inv.Batch?.ProcessingBatchEvaluations?.FirstOrDefault();

            // Lấy thông tin vùng trồng từ Crop thay vì địa chỉ nông dân
            string growingRegion = "";

            if (isFreshCoffee && inv.Detail?.Crop != null)
            {
                // Ưu tiên địa chỉ vùng trồng từ Crop (cho cà phê tươi)
                growingRegion = inv.Detail.Crop.Address ?? "";
                
                // Nếu Crop.Address null, fallback về Farmer.FarmLocation
                if (string.IsNullOrEmpty(growingRegion) && 
                    inv.Detail?.CropSeason?.Farmer != null)
                {
                    growingRegion = inv.Detail.CropSeason.Farmer.FarmLocation ?? "";
                }
            }
            else if (isProcessedCoffee && inv.Batch?.CropSeason != null)
            {
                // Cho cà phê sơ chế, lấy từ CropSeason -> CropSeasonDetails -> Crop -> Address
                var cropSeasonDetails = inv.Batch.CropSeason.CropSeasonDetails?.FirstOrDefault();

                if (cropSeasonDetails?.Crop != null)
                {
                    growingRegion = cropSeasonDetails.Crop.Address ?? "";
                }
                
                // Nếu không có Crop.Address, fallback về Farmer.FarmLocation
                if (string.IsNullOrEmpty(growingRegion) && 
                    inv.Batch?.CropSeason?.Farmer != null)
                {
                    growingRegion = inv.Batch.CropSeason.Farmer.FarmLocation ?? "";
                }
            }

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
                UpdatedAt = inv.UpdatedAt,

                // Sử dụng vùng trồng
                FarmerId = farmer?.FarmerId,
                FarmerName = farmer?.User.Name,
                FarmLocation = growingRegion, // Thay đổi từ farmer?.FarmLocation thành growingRegion
                EvaluationResult = evaluation?.EvaluationResult,
                TotalScore = evaluation?.TotalScore,
                // CropId để frontend có thể gọi Crop API
                CropId = isFreshCoffee 
                    ? inv.Detail?.CropId 
                    : (isProcessedCoffee 
                        ? inv.Batch?.CropSeason?.CropSeasonDetails?.FirstOrDefault()?.CropId 
                        : null)
            };
        }
    }
}
