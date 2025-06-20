using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropSeasonMapper
    {
        public static CropSeasonViewAllDto MapToCropSeasonViewAllDto(this CropSeason entity)
        {
            var status = Enum.TryParse<CropSeasonStatus>(entity.Status, true, out var parsedStatus)
     ? parsedStatus
     : CropSeasonStatus.Active;
            return new CropSeasonViewAllDto
            {
                CropSeasonId = entity.CropSeasonId,
                SeasonName = entity.SeasonName,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Area = entity.Area,
                FarmerName = entity.Farmer?.User?.Name ?? string.Empty,
                Status = status

            };
        }

        public static CropSeasonViewDetailsDto MapToCropSeasonViewDetailsDto(this CropSeason entity)
        {
            var status = Enum.TryParse<CropSeasonStatus>(entity.Status, true, out var parsedStatus)
      ? parsedStatus
      : CropSeasonStatus.Active;
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
                RegistrationId = entity.RegistrationId,
                                Status = status
            };
        }
        public static CropSeason MapToCropSeasonCreateDto(this CropSeasonCreateDto dto, string code)
        {
            return new CropSeason
            {
                CropSeasonId = Guid.NewGuid(),
                CropSeasonCode = code,
                FarmerId = dto.FarmerId,
                RegistrationId = dto.RegistrationId,
                CommitmentId = dto.CommitmentId,
                SeasonName = dto.SeasonName,
                //Area = dto.Area,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Note = dto.Note,
                Status = dto.Status.ToString(), 
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CropSeasonDetails = dto.Details.Select(detail => new CropSeasonDetail
                {
                    DetailId = Guid.NewGuid(),
                    CoffeeTypeId = detail.CoffeeTypeId,
                    ExpectedHarvestStart = detail.ExpectedHarvestStart,
                    ExpectedHarvestEnd = detail.ExpectedHarvestEnd,
                    EstimatedYield = detail.EstimatedYield,
                    AreaAllocated = detail.AreaAllocated,
                    PlannedQuality = detail.PlannedQuality,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Status = dto.Status.ToString() 
                }).ToList()
            };
        }
        public static void MapToExistingEntity(this CropSeasonUpdateDto dto, CropSeason entity)
        {
            entity.SeasonName = dto.SeasonName;
            entity.RegistrationId = dto.RegistrationId;
            entity.CommitmentId = dto.CommitmentId;
            entity.Area = dto.Area;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.Note = dto.Note;
            entity.UpdatedAt = DateTime.Now;
        }


    }
}
