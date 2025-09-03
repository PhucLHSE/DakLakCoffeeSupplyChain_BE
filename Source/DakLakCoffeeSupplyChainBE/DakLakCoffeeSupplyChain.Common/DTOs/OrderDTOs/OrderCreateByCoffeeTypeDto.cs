using DakLakCoffeeSupplyChain.Common.Enum.OrderEnums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs
{
    public class OrderCreateByCoffeeTypeDto
    {
        // Thông tin hợp đồng và loại cà phê
        [Required(ErrorMessage = "ContractId là bắt buộc.")]
        public Guid ContractId { get; set; }

        [Required(ErrorMessage = "CoffeeTypeId là bắt buộc.")]
        public Guid CoffeeTypeId { get; set; }

        // Thông tin giao hàng
        public int? DeliveryRound { get; set; }

        // Thời gian
        public DateTime? OrderDate { get; set; }
        public DateOnly? ActualDeliveryDate { get; set; }

        // Thông tin sản phẩm
        [Required(ErrorMessage = "Số lượng yêu cầu là bắt buộc.")]
        [Range(0.1, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public double RequestedQuantity { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
        public double UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giảm giá phải lớn hơn hoặc bằng 0.")]
        public double? DiscountAmount { get; set; }

        // Ghi chú & trạng thái
        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(OrderStatus), ErrorMessage = "Trạng thái không hợp lệ.")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [MaxLength(500, ErrorMessage = "Lý do huỷ không được vượt quá 500 ký tự.")]
        public string? CancelReason { get; set; }

        // Người tạo
        [JsonIgnore]
        public Guid CreatedBy { get; set; }
    }
}
