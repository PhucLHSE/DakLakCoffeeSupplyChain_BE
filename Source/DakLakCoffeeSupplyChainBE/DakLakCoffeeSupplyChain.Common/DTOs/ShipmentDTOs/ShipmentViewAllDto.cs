using DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs
{
    public class ShipmentViewAllDto
    {
        public Guid ShipmentId { get; set; }

        public string ShipmentCode { get; set; } = string.Empty;

        public Guid OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public Guid DeliveryStaffId { get; set; }

        public string DeliveryStaffName { get; set; } = string.Empty;

        public double? ShippedQuantity { get; set; }

        public DateTime? ShippedAt { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShipmentDeliveryStatus DeliveryStatus { get; set; } = ShipmentDeliveryStatus.Pending;

        public DateTime? ReceivedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        // Thông tin kho (có thể có nhiều kho)
        public List<WarehouseInfoDto> Warehouses { get; set; } = new List<WarehouseInfoDto>();
        
        // Thông tin bên nhận hàng (BusinessBuyer)
        public string BuyerCompanyName { get; set; } = string.Empty;
        public string BuyerContactPerson { get; set; } = string.Empty;
        public string BuyerCompanyAddress { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        
        // Thêm shipmentDetails để hiển thị danh sách sản phẩm
        public List<ShipmentDetailViewDto> ShipmentDetails { get; set; } = new();
    }
}
