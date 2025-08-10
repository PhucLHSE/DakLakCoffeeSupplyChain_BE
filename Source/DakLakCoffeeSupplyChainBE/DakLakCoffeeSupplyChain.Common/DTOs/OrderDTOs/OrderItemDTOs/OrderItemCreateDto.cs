using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs
{
    public class OrderItemCreateDto : IValidatableObject
    {
        //[Required(ErrorMessage = "OrderId là bắt buộc.")]
        public Guid OrderId { get; set; }

        [Required(ErrorMessage = "ContractDeliveryItemId là bắt buộc.")]
        public Guid ContractDeliveryItemId { get; set; }

        [Required(ErrorMessage = "ProductId là bắt buộc.")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        public double? Quantity { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc.")]
        public double? UnitPrice { get; set; }

        public double? DiscountAmount { get; set; } = 0.0;

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        // Validation nghiệp vụ
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Quantity is null || 
                Quantity <= 0)
            {
                yield return new ValidationResult(
                    "Số lượng phải lớn hơn 0.",
                    new[] { nameof(Quantity) }
                );
            }

            if (UnitPrice is null 
                || UnitPrice <= 0)
            {
                yield return new ValidationResult(
                    "Đơn giá phải lớn hơn 0.",
                    new[] { nameof(UnitPrice) }
                );
            }

            if (DiscountAmount < 0)
            {
                yield return new ValidationResult(
                    "Giảm giá không được âm.",
                    new[] { nameof(DiscountAmount) }
                );
            }

            if (Quantity != null && 
                UnitPrice != null && 
                DiscountAmount > Quantity * UnitPrice)
            {
                yield return new ValidationResult(
                    "Giảm giá không được vượt quá tổng thành tiền.",
                    new[] { nameof(DiscountAmount) }
                );
            }
        }
    }
}
