using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System.Numerics;

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
                WantedPrice = cultivation.WantedPrice,
            };
        }
    }
}
