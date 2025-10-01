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
    public class PaymentConfigurationViewAllDto
    {
        public Guid ConfigId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FeeType FeeType { get; set; } = FeeType.Other;

        public double Amount { get; set; }

        public double? MinTons { get; set; }

        public double? MaxTons { get; set; }

        public string ConfigName { get; set; } = string.Empty;

        public bool? IsActive { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }
    }
}
