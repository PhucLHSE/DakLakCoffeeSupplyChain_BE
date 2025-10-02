using DakLakCoffeeSupplyChain.Common.Enum.PaymentConfigurationEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDTOs
{
    public class PaymentConfigurationUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "ConfigId là bắt buộc.")]
        public Guid ConfigId { get; set; }

        [Required(ErrorMessage = "RoleId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "RoleId phải lớn hơn 0.")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "FeeType là bắt buộc.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FeeType FeeType { get; set; } = FeeType.Other;

        [Required(ErrorMessage = "Amount là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount phải là số không âm.")]
        public double Amount { get; set; }

        [StringLength(200, ErrorMessage = "Tên cấu hình không được vượt quá 200 ký tự.")]
        public string? ConfigName { get; set; }

        public double? MinTons { get; set; }

        public double? MaxTons { get; set; }

        [StringLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "EffectiveFrom là bắt buộc.")]
        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public bool? IsActive { get; set; }

        // Validation nghiệp vụ
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinTons.HasValue &&
                MaxTons.HasValue &&
                MinTons > MaxTons)
            {
                yield return new ValidationResult(
                    "Số tấn tối thiểu phải nhỏ hơn hoặc bằng số tấn tối đa.",
                    new[] { nameof(MinTons), nameof(MaxTons) });
            }

            if (EffectiveTo.HasValue &&
                EffectiveTo < EffectiveFrom)
            {
                yield return new ValidationResult(
                    "Ngày hết hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu hiệu lực.",
                    new[] { nameof(EffectiveFrom), nameof(EffectiveTo) });
            }
        }
    }
}
