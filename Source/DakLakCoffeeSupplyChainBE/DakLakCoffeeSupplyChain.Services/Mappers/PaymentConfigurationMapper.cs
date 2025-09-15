using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDtos;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class PaymentConfigurationMapper
    {
        // Mapper PaymentConfigurationViewAllDto
        public static PaymentConfigurationViewAllDto MapToPaymentConfigurationsViewAllDto(
            this PaymentConfiguration config)
        {
            return new PaymentConfigurationViewAllDto
            {
                ConfigId = config.ConfigId,
                RoleName = config.Role?.RoleName ?? "N/A",
                FeeType = config.FeeType,
                Amount = config.Amount,
                IsActive = config.IsActive,
                EffectiveFrom = config.EffectiveFrom,
                EffectiveTo = config.EffectiveTo
            };
        }
    }
}
