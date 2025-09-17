using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDtos
{
    public class PaymentConfigurationViewDetailsDto
    {
        public Guid ConfigId { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string FeeType { get; set; } = string.Empty;

        public double Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool? IsActive { get; set; }
    }
}
