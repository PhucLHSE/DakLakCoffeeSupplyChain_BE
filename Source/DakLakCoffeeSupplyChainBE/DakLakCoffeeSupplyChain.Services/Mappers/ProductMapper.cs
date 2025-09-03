using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
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
    public static class ProductMapper
    {
        // Mapper ProductViewAllDto
        public static ProductViewAllDto MapToProductViewAllDto(this Product product)
        {
            // Parse Status string to enum
            ProductStatus status = Enum.TryParse<ProductStatus>(product.Status, ignoreCase: true, out var parsedStatus)
                   ? parsedStatus
                   : ProductStatus.Pending;

            // Parse Unit string to enum
            ProductUnit unit = Enum.TryParse<ProductUnit>(product.Unit, ignoreCase: true, out var parsedUnit)
                ? parsedUnit
                : ProductUnit.Kg;

            // 🔧 FIX: Tính QuantityAvailable real-time thay vì dùng field static
            double? realTimeQuantityAvailable = CalculateRealTimeQuantityAvailable(product);

            return new ProductViewAllDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                UnitPrice = product.UnitPrice,
                QuantityAvailable = realTimeQuantityAvailable, // Sử dụng giá trị tính toán real-time
                Unit = unit,
                OriginRegion = product.OriginRegion,
                EvaluatedQuality = product.EvaluatedQuality,
                EvaluationScore = product.EvaluationScore,
                Status = status,
                CreatedAt = product.CreatedAt,
                CoffeeTypeName = product.CoffeeType?.TypeName ?? string.Empty,
                InventoryLocation = product.Inventory?.Warehouse?.Location ?? string.Empty,
                BatchCode = product.Batch?.BatchCode ?? string.Empty
            };
        }

        // Mapper ProductViewDetailsDto
        public static ProductViewDetailsDto MapToProductViewDetailsDto(this Product product)
        {
            // Parse Status string to enum
            ProductStatus status = Enum.TryParse<ProductStatus>(product.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ProductStatus.Pending;

            // Parse Unit string to enum
            ProductUnit unit = Enum.TryParse<ProductUnit>(product.Unit, ignoreCase: true, out var parsedUnit)
                ? parsedUnit
                : ProductUnit.Kg;

            // 🔧 FIX: Tính QuantityAvailable real-time thay vì dùng field static
            double? realTimeQuantityAvailable = CalculateRealTimeQuantityAvailable(product);

            return new ProductViewDetailsDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Description = product.Description,
                UnitPrice = product.UnitPrice,
                QuantityAvailable = realTimeQuantityAvailable, // Sử dụng giá trị tính toán real-time
                Unit = unit,
                OriginRegion = product.OriginRegion,
                OriginFarmLocation = product.OriginFarmLocation,
                GeographicalIndicationCode = product.GeographicalIndicationCode,
                CertificationUrl = product.CertificationUrl,
                EvaluatedQuality = product.EvaluatedQuality,
                EvaluationScore = product.EvaluationScore,
                Status = status,
                ApprovalNote = product.ApprovalNote,
                ApprovedByName = product.ApprovedByNavigation?.Name ?? string.Empty,
                ApprovedAt = product.ApprovedAt,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                CoffeeTypeName = product.CoffeeType?.TypeName ?? string.Empty,
                InventoryCode = product.Inventory?.InventoryCode ?? string.Empty,
                InventoryLocation = product.Inventory?.Warehouse?.Location ?? string.Empty,
                WarehouseName = product.Inventory?.Warehouse?.Name ?? string.Empty,
                BatchCode = product.Batch?.BatchCode ?? string.Empty,
                FarmerName = product.Batch?.Farmer?.User?.Name ?? string.Empty
            };
        }

        // Mapper ProductCreateDto => Product
        public static Product MapToNewProduct(this ProductCreateDto productCreateDto, string productCode, Guid createdBy)
        {
            return new Product
            {
                ProductId = Guid.NewGuid(),
                ProductCode = productCode,
                ProductName = productCreateDto.ProductName,
                Description = productCreateDto.Description,
                UnitPrice = productCreateDto.UnitPrice,
                QuantityAvailable = productCreateDto.QuantityAvailable,
                Unit = productCreateDto.Unit.ToString(), // enum sang string để lưu DB
                BatchId = productCreateDto.BatchId,
                InventoryId = productCreateDto.InventoryId,
                CoffeeTypeId = productCreateDto.CoffeeTypeId,
                OriginRegion = productCreateDto.OriginRegion,
                OriginFarmLocation = productCreateDto.OriginFarmLocation,
                GeographicalIndicationCode = productCreateDto.GeographicalIndicationCode,
                CertificationUrl = productCreateDto.CertificationUrl,
                EvaluatedQuality = productCreateDto.EvaluatedQuality,
                EvaluationScore = productCreateDto.EvaluationScore,
                Status = productCreateDto.Status.ToString(), // chuyển enum về string để lưu
                ApprovalNote = productCreateDto.ApprovalNote,
                ApprovedBy = productCreateDto.Status == ProductStatus.Approved ? productCreateDto.ApprovedBy : null,
                ApprovedAt = productCreateDto.Status == ProductStatus.Approved ? productCreateDto.ApprovedAt : null,
                CreatedBy = createdBy,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Mapper ProductUpdateDto => Product
        public static void MapToUpdateProduct(this ProductUpdateDto dto, Product product)
        {
            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.UnitPrice = dto.UnitPrice;
            product.QuantityAvailable = dto.QuantityAvailable;
            product.Unit = dto.Unit.ToString(); // enum sang string
            product.BatchId = dto.BatchId;
            product.InventoryId = dto.InventoryId;
            product.CoffeeTypeId = dto.CoffeeTypeId;
            product.OriginRegion = dto.OriginRegion;
            product.OriginFarmLocation = dto.OriginFarmLocation;
            product.GeographicalIndicationCode = dto.GeographicalIndicationCode;
            product.CertificationUrl = dto.CertificationUrl;
            product.EvaluatedQuality = dto.EvaluatedQuality;
            product.EvaluationScore = dto.EvaluationScore;
            product.Status = dto.Status.ToString(); // enum sang string
            product.ApprovalNote = dto.ApprovalNote;

            // Nếu trạng thái là Approved thì cập nhật ApprovedBy và ApprovedAt
            if (dto.Status == ProductStatus.Approved)
            {
                product.ApprovedBy = dto.ApprovedBy;
                product.ApprovedAt = dto.ApprovedAt;
            }
            else
            {
                product.ApprovedBy = null;
                product.ApprovedAt = null;
            }

            product.UpdatedAt = DateHelper.NowVietnamTime();
        }

        /// <summary>
        /// Tính toán QuantityAvailable real-time dựa trên Inventory thực tế và OrderItems đã đặt
        /// </summary>
        /// <param name="product">Product entity</param>
        /// <returns>Số lượng khả dụng thực tế</returns>
        private static double? CalculateRealTimeQuantityAvailable(Product product)
        {
            try
            {
                // 1. Lấy số lượng tồn kho thực tế từ Inventory
                double inventoryQuantity = 0;
                if (product.Inventory != null)
                {
                    inventoryQuantity = product.Inventory.Quantity;
                }

                // 2. Nếu không có Inventory, sử dụng giá trị cũ (fallback)
                if (inventoryQuantity <= 0)
                {
                    return product.QuantityAvailable;
                }

                // 3. Trừ đi số lượng đã đặt trong các order (nếu có)
                // Lưu ý: Logic này cần được implement trong ProductService vì cần truy cập database
                // Ở đây chỉ là placeholder, thực tế sẽ được tính trong service layer
                
                // 🔧 TODO: Cần implement logic trừ OrderItems trong ProductService
                // double reservedQuantity = GetReservedQuantityFromOrders(product.ProductId);
                // double realTimeAvailable = Math.Max(0, inventoryQuantity - reservedQuantity);
                
                // Tạm thời trả về inventory quantity thực tế
                return inventoryQuantity;
            }
            catch (Exception)
            {
                // Nếu có lỗi, fallback về giá trị cũ
                return product.QuantityAvailable;
            }
        }
    }
}
