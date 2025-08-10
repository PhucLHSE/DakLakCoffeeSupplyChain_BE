using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.OrderEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs
{
    public class OrderCreateDto : IValidatableObject
    {
        // Thông tin giao hàng
        [Required(ErrorMessage = "DeliveryBatchId là bắt buộc.")]
        public Guid DeliveryBatchId { get; set; }

        public int? DeliveryRound { get; set; }

        // Thời gian
        public DateTime? OrderDate { get; set; }

        public DateOnly? ActualDeliveryDate { get; set; }

        // Ghi chú & trạng thái
        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(OrderStatus), ErrorMessage = "Trạng thái không hợp lệ.")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [MaxLength(500, ErrorMessage = "Lý do huỷ không được vượt quá 500 ký tự.")]
        public string? CancelReason { get; set; }

        // Người tạo
        //[Required(ErrorMessage = "Người tạo là bắt buộc.")]
        [JsonIgnore]
        public Guid CreatedBy { get; set; }

        // Danh sách sản phẩm
        public List<OrderItemCreateDto> OrderItems { get; set; } = new();

        // Validation nghiệp vụ
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra danh sách OrderItems
            if (OrderItems == null || 
                !OrderItems.Any())
            {
                yield return new ValidationResult(
                    "Đơn hàng phải có ít nhất một sản phẩm.",
                    new[] { nameof(OrderItems) }
                );
            }

            var duplicateProductIds = OrderItems
                .GroupBy(item => item.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateProductIds.Any())
            {
                yield return new ValidationResult(
                    $"Có sản phẩm bị trùng lặp trong đơn hàng: {string.Join(", ", duplicateProductIds)}",
                    new[] { nameof(OrderItems) }
                );
            }

            // Validate DeliveryRound > 0 nếu có
            if (DeliveryRound.HasValue && 
                DeliveryRound <= 0)
            {
                yield return new ValidationResult(
                    "Đợt giao hàng phải lớn hơn 0 nếu có.",
                    new[] { nameof(DeliveryRound) }
                );
            }

            // Validate OrderDate không vượt hiện tại
            if (OrderDate.HasValue && 
                OrderDate > DateTime.UtcNow.AddDays(1))
            {
                yield return new ValidationResult(
                    "Ngày đặt hàng không được vượt quá ngày hiện tại.",
                    new[] { nameof(OrderDate) }
                );
            }

            // Validate ActualDeliveryDate không trước OrderDate và không vượt quá hiện tại
            if (ActualDeliveryDate.HasValue)
            {
                var actual = ActualDeliveryDate.Value.ToDateTime(TimeOnly.MinValue);

                if (OrderDate.HasValue && 
                    actual < OrderDate.Value)
                {
                    yield return new ValidationResult(
                        "Ngày giao thực tế không được trước ngày đặt hàng.",
                        new[] { nameof(ActualDeliveryDate) }
                    );
                }

                if (actual > DateTime.UtcNow.AddDays(1))
                {
                    yield return new ValidationResult(
                        "Ngày giao thực tế không được vượt quá hiện tại.",
                        new[] { nameof(ActualDeliveryDate) }
                    );
                }
            }
        }
    }
}
