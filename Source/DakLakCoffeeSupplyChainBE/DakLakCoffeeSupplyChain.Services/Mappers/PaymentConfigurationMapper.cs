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

        // Mapper PaymentConfigurationViewDetailsDto
        public static PaymentConfigurationViewDetailsDto MapToPaymentConfigurationViewDetailsDto(
            this PaymentConfiguration config)
        {
            return new PaymentConfigurationViewDetailsDto
            {
                ConfigId = config.ConfigId,
                RoleId = config.RoleId,
                RoleName = config.Role?.RoleName ?? "N/A",
                FeeType = config.FeeType,
                Amount = config.Amount,
                Description = config.Description,
                EffectiveFrom = config.EffectiveFrom,
                EffectiveTo = config.EffectiveTo,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt,
                IsActive = config.IsActive
            };
        }
    }
}
