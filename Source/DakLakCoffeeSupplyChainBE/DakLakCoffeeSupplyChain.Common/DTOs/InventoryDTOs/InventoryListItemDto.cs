using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs
{
    public class InventoryListItemDto
    {
        public Guid InventoryId { get; set; }
        public string InventoryCode { get; set; } = default!;
        public Guid WarehouseId { get; set; }
        public Guid? BatchId { get; set; }
        public string WarehouseName { get; set; } = default!;
        // Thêm thông tin vị trí kho
        public string? WarehouseLocation { get; set; }
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
        
        // Thông tin FIFO
        public DateTime CreatedAt { get; set; }
        public int FifoPriority { get; set; } // Thứ tự ưu tiên FIFO (1 = cao nhất)
        public bool IsRecommended { get; set; } // Có được khuyến nghị xuất trước không
        public string FifoRecommendation { get; set; } = ""; // Lý do khuyến nghị
    }
}
