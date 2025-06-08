using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
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
            ProductStatus status = Enum.TryParse<ProductStatus>(product.Status, out var parsedStatus)
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
    }
}
