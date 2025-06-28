using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem
{
    public class ContractDeliveryItemCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "DeliveryBatchId là bắt buộc.")]
        public Guid DeliveryBatchId { get; set; }

        [Required(ErrorMessage = "ContractItemId là bắt buộc.")]
        public Guid ContractItemId { get; set; }

        [Required(ErrorMessage = "Số lượng dự kiến là bắt buộc.")]
        public double? PlannedQuantity { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string? Note { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PlannedQuantity is null || PlannedQuantity <= 0)
            {
                yield return new ValidationResult(
                    "Số lượng dự kiến phải lớn hơn 0.",
                    new[] { nameof(PlannedQuantity) }
                );
            }
        }
    }
}
