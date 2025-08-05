using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropSeasonMapper
    {
        public static CropSeasonViewAllDto MapToCropSeasonViewAllDto(this CropSeason entity)
        {
            Enum.TryParse(entity.Status, true, out CropSeasonStatus status);

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

        public static CropSeasonViewDetailsDto MapToCropSeasonViewDetailsDto(this CropSeason entity, CultivationRegistration? registration)
        {
            Enum.TryParse(entity.Status, true, out CropSeasonStatus status);

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
                RegistrationId = registration?.RegistrationId ?? Guid.Empty,
                RegistrationCode = registration?.RegistrationCode ?? string.Empty,
                Status = status,
                Details = entity.CropSeasonDetails?
                    .Where(d => !d.IsDeleted)
                    .Select(d => d.MapToCropSeasonDetailViewDto(entity))
                    .ToList() ?? new List<CropSeasonDetailViewDto>()
            };
        }

        public static CropSeasonDetailViewDto MapToCropSeasonDetailViewDto(this CropSeasonDetail detail, CropSeason parent)
        {
            Enum.TryParse(detail.Status, true, out CropDetailStatus status);

            return new CropSeasonDetailViewDto
            {
                DetailId = detail.DetailId,
                AreaAllocated = detail.AreaAllocated ?? 0,
                ExpectedHarvestStart = detail.ExpectedHarvestStart,
                ExpectedHarvestEnd = detail.ExpectedHarvestEnd,
                EstimatedYield = detail.EstimatedYield,
                ActualYield = detail.ActualYield ?? 0, 
                FarmerId = parent.FarmerId,
                FarmerName = parent.Farmer?.User?.Name ?? "Không rõ",
                PlannedQuality = detail.PlannedQuality ?? string.Empty,
                Status = status,
                CommitmentDetailId = detail.CommitmentDetailId,
                CommitmentDetailCode = detail.CommitmentDetail?.CommitmentDetailCode ?? string.Empty,
                CoffeeTypeId = detail.CommitmentDetail?.PlanDetail?.CoffeeTypeId ?? Guid.Empty,
                TypeName = detail.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? string.Empty,
                ConfirmedPrice = detail.CommitmentDetail?.ConfirmedPrice ?? 0,
                CommittedQuantity = detail.CommitmentDetail?.CommittedQuantity ?? 0
            };
        }

        public static CropSeason MapToCropSeasonCreateDto(
            this CropSeasonCreateDto dto,
            string code,
            Guid farmerId,
            Guid cropSeasonId
        )
        {
            return new CropSeason
            {
                CropSeasonId = cropSeasonId,
                CropSeasonCode = code,
                FarmerId = farmerId,
                CommitmentId = dto.CommitmentId,
                SeasonName = dto.SeasonName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Note = dto.Note,
                Status = CropSeasonStatus.Active.ToString(),
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
            };
        }

        public static void MapToExistingEntity(this CropSeasonUpdateDto dto, CropSeason entity)
        {
            entity.SeasonName = dto.SeasonName;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.Note = dto.Note;
            entity.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
