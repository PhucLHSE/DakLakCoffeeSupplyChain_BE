using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs
{
    public class ProductViewAllDto
    {
        public Guid ProductId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public double? UnitPrice { get; set; }

        public double? QuantityAvailable { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductUnit Unit { get; set; }

        public string OriginRegion { get; set; } = string.Empty;

        public string EvaluatedQuality { get; set; } = string.Empty;

        public double? EvaluationScore { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        public DateTime CreatedAt { get; set; }

        public string CoffeeTypeName { get; set; } = string.Empty; // Lấy từ CoffeeType

        public string InventoryLocation { get; set; } = string.Empty; // Lấy từ Inventory

        public string BatchCode { get; set; } = string.Empty; // Lấy từ ProcessingBatch
    }
}
