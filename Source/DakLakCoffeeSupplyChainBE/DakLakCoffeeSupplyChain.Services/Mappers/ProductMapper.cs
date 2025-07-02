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

            return new ProductViewAllDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                UnitPrice = product.UnitPrice,
                QuantityAvailable = product.QuantityAvailable,
                Unit = product.Unit,
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
            ProductStatus status = Enum.TryParse<ProductStatus>(product.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ProductStatus.Pending;

            return new ProductViewDetailsDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Description = product.Description,
                UnitPrice = product.UnitPrice,
                QuantityAvailable = product.QuantityAvailable,
                Unit = product.Unit,
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
                InventoryLocation = product.Inventory?.Warehouse?.Location ?? string.Empty,
                BatchCode = product.Batch?.BatchCode ?? string.Empty
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
    }
}
