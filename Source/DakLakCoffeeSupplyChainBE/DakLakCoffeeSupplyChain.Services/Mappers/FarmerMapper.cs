using DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class FarmerMapper
    {
        // Mapper FarmerViewAllDto
        public static FarmerViewAllDto MapToFarmerViewAllDto(this Farmer farmer)
        {
            return new FarmerViewAllDto
            {
                FarmerId = farmer.FarmerId,
                FarmerCode = farmer.FarmerCode,
                FarmLocation = farmer.FarmLocation,
                UserId = farmer.UserId,
                FarmerName = farmer.User.Name
            };
        }
    }
}
