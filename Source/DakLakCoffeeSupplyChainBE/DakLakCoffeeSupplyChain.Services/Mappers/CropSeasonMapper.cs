using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
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
                FarmerId = entity.FarmerId,
                Status = status
            };
        }

        public static CropSeasonViewDetailsDto MapToCropSeasonViewDetailsDto(this CropSeason entity)
        {
            var status = Enum.TryParse<CropSeasonStatus>(entity.Status, true, out var parsedStatus)
                ? parsedStatus : CropSeasonStatus.Active;

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
                CommitmentName = entity.Commitment?.CommitmentName ?? string.Empty,
                RegistrationId = entity.RegistrationId,
                RegistrationCode = entity.Registration?.RegistrationCode ?? string.Empty,
                Status = status,

                Details = entity.CropSeasonDetails?
                    .Where(d => !d.IsDeleted)
                    .Select(d => new CropSeasonDetailViewDto
                    {
                        DetailId = d.DetailId,
                        CoffeeTypeId = d.CoffeeTypeId,
                        TypeName = d.CoffeeType?.TypeName ?? string.Empty,
                        AreaAllocated = d.AreaAllocated ?? 0,
                        ExpectedHarvestStart = d.ExpectedHarvestStart,
                        ExpectedHarvestEnd = d.ExpectedHarvestEnd,
                        EstimatedYield = d.EstimatedYield,
                        FarmerId = entity.FarmerId,
                        FarmerName = entity.Farmer?.User?.Name ?? "Không rõ",
                        PlannedQuality = d.PlannedQuality ?? string.Empty,
                        Status = Enum.TryParse<CropDetailStatus>(d.Status, out var detailStatus) ? detailStatus : CropDetailStatus.Planned
                    }).ToList() ?? new List<CropSeasonDetailViewDto>()
            };
        }

        public static CropSeason MapToCropSeasonCreateDto(
          this CropSeasonCreateDto dto,
          string code,
          Guid farmerId,
          Guid registrationId // 👈 thêm vào đây
      )
        {
            return new CropSeason
            {
                CropSeasonId = Guid.NewGuid(),
                CropSeasonCode = code,
                FarmerId = farmerId,
                RegistrationId = registrationId, // 👈 sử dụng tham số này
                CommitmentId = dto.CommitmentId,
                SeasonName = dto.SeasonName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Note = dto.Note,
                Status = dto.Status.ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };
        }


        public static void MapToExistingEntity(this CropSeasonUpdateDto dto, CropSeason entity)
        {
            entity.SeasonName = dto.SeasonName;
            entity.CommitmentId = dto.CommitmentId;
            entity.Area = dto.Area;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.Note = dto.Note;
            entity.Status = dto.Status.ToString();
            entity.UpdatedAt = DateTime.Now;
        }

    
    }
}
