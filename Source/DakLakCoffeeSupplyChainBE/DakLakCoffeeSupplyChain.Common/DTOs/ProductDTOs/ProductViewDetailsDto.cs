using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs
{
    public class ProductViewDetailsDto
    {
        public Guid ProductId { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public double? UnitPrice { get; set; }

        public double? QuantityAvailable { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductUnit Unit { get; set; }

        public string OriginRegion { get; set; } = string.Empty;

        public string OriginFarmLocation { get; set; } = string.Empty;

        public string GeographicalIndicationCode { get; set; } = string.Empty;

        public string CertificationUrl { get; set; } = string.Empty;

        public string EvaluatedQuality { get; set; } = string.Empty;

        public double? EvaluationScore { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        public string ApprovalNote { get; set; } = string.Empty;

        public string ApprovedByName { get; set; } = string.Empty; // UserAccount.FullName

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string CoffeeTypeName { get; set; } = string.Empty;

        public string InventoryCode { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;

        public string InventoryLocation { get; set; } = string.Empty;

        public string BatchCode { get; set; } = string.Empty;
    }
}
