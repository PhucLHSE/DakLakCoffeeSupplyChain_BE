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
                FarmerName = farmer.User.Name,
                IsVerified = farmer.IsVerified
            };
        }

        // Mapper FarmerViewDetailsDto
        public static FarmerViewDetailsDto MapToFarmerViewDetailsDto(this Farmer entity)
        {
            return new FarmerViewDetailsDto
            {
                FarmerId = entity.FarmerId,
                FarmerCode = entity.FarmerCode,
                UserId = entity.UserId,
                FarmerName = entity.User.Name,
                FarmLocation = entity.FarmLocation,
                FarmSize = entity.FarmSize,
                CertificationStatus = entity.CertificationStatus,
                CertificationUrl = entity.CertificationUrl,
                IsVerified = entity.IsVerified,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsDeleted = entity.IsDeleted
            };
        }
    }
}
