using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs
{
    public class ShipmentDetailCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ShipmentId là bắt buộc.")]
        public Guid ShipmentId { get; set; }

        [Required(ErrorMessage = "OrderItemId là bắt buộc.")]
        public Guid OrderItemId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        public double? Quantity { get; set; }

        [Required(ErrorMessage = "Đơn vị tính là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(ProductUnit), ErrorMessage = "Đơn vị tính không hợp lệ (Kg, Ta, Tan).")]
        public ProductUnit Unit { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string Note { get; set; } = string.Empty;

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
        }
    }
}
