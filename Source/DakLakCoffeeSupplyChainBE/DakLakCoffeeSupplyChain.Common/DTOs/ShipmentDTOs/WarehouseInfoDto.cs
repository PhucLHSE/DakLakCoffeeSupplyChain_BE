using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs
{
    // DTO cho thông tin kho
    public class WarehouseInfoDto
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double? Capacity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
