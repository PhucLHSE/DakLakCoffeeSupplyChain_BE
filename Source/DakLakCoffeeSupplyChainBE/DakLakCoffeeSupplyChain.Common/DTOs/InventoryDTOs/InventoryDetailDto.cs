using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs
{
    public class InventoryDetailDto
    {
        public Guid InventoryId { get; set; }

        public string InventoryCode { get; set; } = default!;

        public Guid WarehouseId { get; set; }

        public string WarehouseName { get; set; } = default!;

        // Thêm thông tin vị trí kho
        public string? WarehouseLocation { get; set; }

        public Guid BatchId { get; set; }

        public string BatchCode { get; set; } = default!;

        public string ProductName { get; set; } = default!;

        public string CoffeeTypeName { get; set; } = default!;
        
        // Thông tin cho cà phê tươi
        public Guid? DetailId { get; set; }

        public string? DetailCode { get; set; }

        public string? CropSeasonName { get; set; }

        public string? CoffeeTypeNameDetail { get; set; }

        public double Quantity { get; set; }

        public string Unit { get; set; } = "kg";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Farmers và ProcessingBatchEvaluations
        public Guid? FarmerId { get; set; }

        public string? FarmerName { get; set; }

        public string? FarmLocation { get; set; }

        public string? EvaluationResult { get; set; }

        public decimal? TotalScore { get; set; }
    }
}
