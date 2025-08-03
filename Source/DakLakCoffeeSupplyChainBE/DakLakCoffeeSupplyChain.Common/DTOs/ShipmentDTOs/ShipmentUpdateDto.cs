using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs
{
    public class ShipmentUpdateDto : IValidatableObject
    {
        // Mã định danh chuyến giao cần cập nhật
        [Required(ErrorMessage = "ShipmentId là bắt buộc.")]
        public Guid ShipmentId { get; set; }

        // Gắn với đơn hàng
        [Required(ErrorMessage = "OrderId là bắt buộc.")]
        public Guid OrderId { get; set; }

        // Nhân viên giao hàng
        [Required(ErrorMessage = "DeliveryStaffId là bắt buộc.")]
        public Guid DeliveryStaffId { get; set; }

        // Số lượng đã giao
        public double? ShippedQuantity { get; set; }

        // Ngày bắt đầu giao hàng
        public DateTime? ShippedAt { get; set; }

        // Trạng thái giao hàng
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(ShipmentDeliveryStatus), ErrorMessage = "Trạng thái giao hàng không hợp lệ.")]
        public ShipmentDeliveryStatus DeliveryStatus { get; set; } = ShipmentDeliveryStatus.Pending;

        // Ngày nhận hàng thành công
        public DateTime? ReceivedAt { get; set; }

        // Danh sách chi tiết sản phẩm trong chuyến giao
        public List<ShipmentDetailUpdateDto> ShipmentDetails { get; set; } = new();

        // Validation nghiệp vụ
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra danh sách chi tiết chuyến giao
            if (ShipmentDetails == null || 
                !ShipmentDetails.Any())
            {
                yield return new ValidationResult(
                    "Cần ít nhất một sản phẩm trong chuyến giao.",
                    new[] { nameof(ShipmentDetails) }
                );
            }

            // Trùng OrderItemId
            var duplicateOrderItems = ShipmentDetails
                .GroupBy(x => x.OrderItemId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateOrderItems.Any())
            {
                yield return new ValidationResult(
                    $"Có sản phẩm bị trùng lặp trong chuyến giao: {string.Join(", ", duplicateOrderItems)}",
                    new[] { nameof(ShipmentDetails) }
                );
            }

            // Validate thời gian giao hàng không vượt quá hiện tại
            if (ShippedAt.HasValue && 
                ShippedAt > DateTime.UtcNow.AddDays(1))
            {
                yield return new ValidationResult(
                    "Ngày bắt đầu giao không được vượt quá hiện tại.",
                    new[] { nameof(ShippedAt) }
                );
            }

            if (ReceivedAt.HasValue && 
                ReceivedAt > DateTime.UtcNow.AddDays(1))
            {
                yield return new ValidationResult(
                    "Ngày nhận hàng không được vượt quá hiện tại.",
                    new[] { nameof(ReceivedAt) }
                );
            }

            if (ShippedAt.HasValue && 
                ReceivedAt.HasValue && 
                ReceivedAt < ShippedAt)
            {
                yield return new ValidationResult(
                    "Ngày nhận hàng không được trước ngày bắt đầu giao.",
                    new[] { nameof(ReceivedAt), nameof(ShippedAt) }
                );
            }

            // Validate số lượng giao
            if (ShippedQuantity.HasValue && 
                ShippedQuantity <= 0)
            {
                yield return new ValidationResult(
                    "Khối lượng đã giao phải lớn hơn 0 nếu có.",
                    new[] { nameof(ShippedQuantity) }
                );
            }
        }
    }
}
