using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class SystemConfigurationMapper
    {
        public static SystemConfigurationViewAllDto MapToSystemConfigurationViewAllDto(this SystemConfiguration config)
        {
            return new SystemConfigurationViewAllDto
            {
                Name = config.Name,
                Description = config?.Description,
                MinValue = config?.MinValue,
                MaxValue = config?.MaxValue,
                Unit = config?.Unit,
                IsActive = config.IsActive,
                EffectedDateFrom = config.EffectedDateFrom,
                EffectedDateTo = config.EffectedDateTo,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt,
                TargetEntity = config.TargetEntity,
                TargetField = config.TargetField,
                Operator = config.Operator,
                ScopeType = config.ScopeType,
                ScopeId = config.ScopeId,
                Severity = config.Severity,
                RuleGroup = config.RuleGroup,
                VersionNo = config.VersionNo,
                CreatedBy = config.CreatedBy,
                UpdatedBy = config.UpdatedBy
            };
        }

        public static SystemConfigurationViewDetailDto MapToSystemConfigurationViewDetailDto(this SystemConfiguration config)
        {
            return new SystemConfigurationViewDetailDto
            {
                Name = config.Name,
                Description = config?.Description,
                MinValue = config?.MinValue,
                MaxValue = config?.MaxValue,
                Unit = config?.Unit,
                IsActive = config.IsActive,
                EffectedDateFrom = config.EffectedDateFrom,
                EffectedDateTo = config.EffectedDateTo,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt,
                TargetEntity = config.TargetEntity,
                TargetField = config.TargetField,
                Operator = config.Operator,
                ScopeType = config.ScopeType,
                ScopeId = config.ScopeId,
                Severity = config.Severity,
                RuleGroup = config.RuleGroup,
                VersionNo = config.VersionNo,
                CreatedBy = config.CreatedBy,
                UpdatedBy = config.UpdatedBy
            };
        }
    }
}
