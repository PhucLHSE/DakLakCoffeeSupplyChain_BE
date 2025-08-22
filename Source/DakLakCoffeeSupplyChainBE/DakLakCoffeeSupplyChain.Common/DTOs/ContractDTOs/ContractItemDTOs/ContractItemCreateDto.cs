using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs
{
    public class ContractItemCreateDto : IValidatableObject
    {
        //[Required(ErrorMessage = "ContractId là bắt buộc.")]
        public Guid? ContractId { get; set; }

        [Required(ErrorMessage = "CoffeeTypeId là bắt buộc.")]
        public Guid CoffeeTypeId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        public double? Quantity { get; set; }

        [Required(ErrorMessage = "Đơn giá là bắt buộc.")]
        public double? UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0% đến 100%.")]
        public double? DiscountAmount { get; set; } = 0.0; // % giảm giá

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string Note { get; set; } = string.Empty;

        // Validation nghiệp vụ
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

            // Validation cho % giảm giá (0-100%)
            if (DiscountAmount.HasValue && 
                (DiscountAmount < 0 || DiscountAmount > 100))
            {
                yield return new ValidationResult(
                    "Phần trăm giảm giá phải từ 0% đến 100%.", 
                    new[] { nameof(DiscountAmount) }
                );
            }
        }
    }
}
