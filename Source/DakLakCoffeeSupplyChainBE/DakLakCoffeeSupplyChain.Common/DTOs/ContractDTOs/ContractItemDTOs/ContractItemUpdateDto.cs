using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs
{
    public class ContractItemUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ContractItemId là bắt buộc.")]
        public Guid ContractItemId { get; set; }

        [Required(ErrorMessage = "ContractId là bắt buộc.")]
        public Guid ContractId { get; set; }

        [Required(ErrorMessage = "CoffeeTypeId là bắt buộc.")]
        public Guid CoffeeTypeId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        public double? Quantity { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc.")]
        public double? UnitPrice { get; set; }

        public double? DiscountAmount { get; set; } = 0.0;

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string Note { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Quantity <= 0)
            {
                yield return new ValidationResult(
                    "Số lượng phải lớn hơn 0.",
                    new[] { nameof(Quantity) }
                );
            }

            if (UnitPrice <= 0)
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
