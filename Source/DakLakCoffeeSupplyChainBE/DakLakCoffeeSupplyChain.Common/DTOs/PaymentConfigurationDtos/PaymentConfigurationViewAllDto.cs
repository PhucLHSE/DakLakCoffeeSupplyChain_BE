using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDtos
{
    public class PaymentConfigurationViewAllDto
    {
        public Guid ConfigId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string FeeType { get; set; } = string.Empty;

        public double Amount { get; set; }

        public bool? IsActive { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }
    }
}
