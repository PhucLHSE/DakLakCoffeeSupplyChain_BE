using DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs
{
    public class ShipmentStatusUpdateDto
    {
        // Trạng thái giao hàng mới
        [Required(ErrorMessage = "Trạng thái giao hàng là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(ShipmentDeliveryStatus), ErrorMessage = "Trạng thái giao hàng không hợp lệ.")]
        public ShipmentDeliveryStatus DeliveryStatus { get; set; }

        // Ngày nhận hàng thành công (chỉ khi status = Delivered)
        public DateTime? ReceivedAt { get; set; }

        // Ghi chú (tùy chọn)
        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }
    }
}
