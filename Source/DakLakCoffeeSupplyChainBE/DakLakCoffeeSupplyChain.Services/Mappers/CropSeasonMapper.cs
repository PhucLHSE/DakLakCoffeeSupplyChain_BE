using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropSeasonMapper
    {
        public static CropSeasonViewAllDto MapToCropSeasonViewAllDto(this CropSeason entity)
        {

            return new CropSeasonViewAllDto
            {
                CropSeasonId = entity.CropSeasonId,
                SeasonName = entity.SeasonName,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Area = entity.Area,
                FarmerName = entity.Farmer?.User?.Name ?? string.Empty
            };
        }

        public static CropSeasonViewDetailsDto MapToCropSeasonViewDetailsDto(this CropSeason entity)
        {
            return new CropSeasonViewDetailsDto
            {
                CropSeasonId = entity.CropSeasonId,
                SeasonName = entity.SeasonName ?? string.Empty,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Area = entity.Area,
                Note = entity.Note ?? string.Empty,
                FarmerId = entity.FarmerId,
                FarmerName = entity.Farmer?.User?.Name ?? string.Empty,
                CommitmentId = entity.CommitmentId,
                RegistrationId = entity.RegistrationId
            };
        }
    }
}
