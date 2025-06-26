using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CultivationRegistrationMapper
    {
        // Mapper CultivationRegistrationViewAllDto
        public static CultivationRegistrationViewAllDto MapToCultivationRegistrationViewAllDto(this CultivationRegistration cultivation)
        {
            return new CultivationRegistrationViewAllDto
            {
                RegistrationId = cultivation.RegistrationId,
                RegistrationCode = cultivation.RegistrationCode,
                PlanId = cultivation.PlanId,
                FarmerId = cultivation.FarmerId,
                FarmerName = cultivation.Farmer.User.Name,
                RegisteredArea = cultivation.RegisteredArea,
                RegisteredAt = cultivation.RegisteredAt,
                TotalWantedPrice = cultivation.TotalWantedPrice,
            };
        }

        // Mapper CultivationRegistrationViewSumaryDto
        public static CultivationRegistrationViewSumaryDto MapToCultivationRegistrationViewSumaryDto(this CultivationRegistration entity)
        {
            return new CultivationRegistrationViewSumaryDto
            {
                RegistrationId = entity.RegistrationId,
                RegistrationCode = entity.RegistrationCode,
                PlanId = entity.PlanId,
                FarmerId = entity.FarmerId,
                FarmerName = entity.Farmer.User.Name,
                RegisteredArea = entity.RegisteredArea,
                RegisteredAt = entity.RegisteredAt,
                TotalWantedPrice = entity.TotalWantedPrice,
                Status = EnumHelper.ParseEnumFromString(entity.Status, CultivationRegistrationStatus.Unknown),
                Note = entity.Note,
                CultivationRegistrationDetails = entity.CultivationRegistrationsDetails?
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CultivationRegistrationViewDetailsDto
                {
                    CultivationRegistrationDetailId = c.CultivationRegistrationDetailId,
                    RegistrationId = c.RegistrationId,
                    PlanDetailId = c.PlanDetailId,
                    EstimatedYield = c.EstimatedYield,
                    ExpectedHarvestStart = c.ExpectedHarvestStart,
                    ExpectedHarvestEnd = c.ExpectedHarvestEnd,
                    Status = EnumHelper.ParseEnumFromString(c.Status, CultivationRegistrationStatus.Unknown),
                    Note = c.Note
                }).ToList() ?? []
            };
        }
    }
}
