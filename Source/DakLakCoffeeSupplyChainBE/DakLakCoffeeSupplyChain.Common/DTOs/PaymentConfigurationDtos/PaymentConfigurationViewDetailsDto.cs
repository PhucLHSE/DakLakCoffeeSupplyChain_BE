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
    public class PaymentConfigurationViewDetailsDto
    {
        public Guid ConfigId { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FeeType FeeType { get; set; } = FeeType.Other;

        public double Amount { get; set; }

        public double? MinTons { get; set; }

        public double? MaxTons { get; set; }

        public string ConfigName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool? IsActive { get; set; }
    }
}
