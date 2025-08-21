using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem
{
    public class ContractDeliveryItemUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "DeliveryItemId là bắt buộc.")]
        public Guid DeliveryItemId { get; set; }

        [Required(ErrorMessage = "ContractItemId là bắt buộc.")]
        public Guid ContractItemId { get; set; }

        [Required(ErrorMessage = "Số lượng dự kiến là bắt buộc.")]
        public double? PlannedQuantity { get; set; }

        public double? FulfilledQuantity { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Planned > 0
            if (PlannedQuantity is null || 
                PlannedQuantity <= 0)
            {
                yield return new ValidationResult(
                    "Số lượng dự kiến phải lớn hơn 0.",
                    new[] { nameof(PlannedQuantity) }
                );
            }

            // Fulfilled >= 0 (nếu có nhập)
            if (FulfilledQuantity is not null 
                && FulfilledQuantity < 0)
            {
                yield return new ValidationResult(
                    "Khối lượng đã giao không được âm.",
                    new[] { nameof(FulfilledQuantity) }
                );
            }

            // Fulfilled <= Planned (khi cả hai đều có giá trị hợp lệ)
            if (PlannedQuantity is not null 
                && PlannedQuantity > 0
                && FulfilledQuantity is not null
                && FulfilledQuantity > PlannedQuantity)
            {
                yield return new ValidationResult(
                    "Khối lượng đã giao phải nhỏ hơn hoặc bằng khối lượng cần giao.",
                    new[] { nameof(FulfilledQuantity), nameof(PlannedQuantity) }
                );
            }
        }
    }
}
