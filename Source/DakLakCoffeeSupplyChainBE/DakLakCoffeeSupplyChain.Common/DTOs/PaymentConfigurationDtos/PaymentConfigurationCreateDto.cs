using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDTOs
{
    public class PaymentConfigurationCreateDto
    {
        [Required(ErrorMessage = "RoleId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "RoleId phải lớn hơn 0.")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "FeeType là bắt buộc.")]
        [StringLength(50, ErrorMessage = "FeeType không được vượt quá 50 ký tự.")]
        public string FeeType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount phải là số không âm.")]
        public double Amount { get; set; }

        [StringLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "EffectiveFrom là bắt buộc.")]
        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public bool? IsActive { get; set; } = true;
    }
}
