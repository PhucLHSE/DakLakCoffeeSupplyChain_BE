using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        // Mapper PaymentConfigurationCreateDto -> PaymentConfiguration
        public static PaymentConfiguration MapToNewPaymentConfiguration(
            this PaymentConfigurationCreateDto dto)
        {
            return new PaymentConfiguration
            {
                ConfigId = Guid.NewGuid(),
                RoleId = dto.RoleId,
                FeeType = dto.FeeType,
                Amount = dto.Amount,
                Description = dto.Description,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsActive = dto.IsActive ?? true,
                IsDeleted = false
            };
        }

        // Mapper PaymentConfigurationUpdateDto -> PaymentConfiguration
        public static void MapToUpdatedPaymentConfiguration(
            this PaymentConfigurationUpdateDto dto,
            PaymentConfiguration paymentConfiguration)
        {
            paymentConfiguration.RoleId = dto.RoleId;
            paymentConfiguration.FeeType = dto.FeeType;
            paymentConfiguration.Amount = dto.Amount;
            paymentConfiguration.Description = dto.Description;
            paymentConfiguration.EffectiveFrom = dto.EffectiveFrom;
            paymentConfiguration.EffectiveTo = dto.EffectiveTo;
            paymentConfiguration.IsActive = dto.IsActive ?? paymentConfiguration.IsActive;
            paymentConfiguration.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
