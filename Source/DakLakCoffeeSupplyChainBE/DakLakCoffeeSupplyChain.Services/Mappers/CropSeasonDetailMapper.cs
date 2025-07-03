using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropSeasonDetailMapper
    {
        public static CropSeasonDetailViewDto MapToCropSeasonDetailViewDto(this CropSeasonDetail entity)
        {
            return new CropSeasonDetailViewDto
            {
                DetailId = entity.DetailId,
                AreaAllocated = entity.AreaAllocated ?? 0,
                CoffeeTypeId = entity.CoffeeTypeId,
                TypeName = entity.CoffeeType?.TypeName ?? string.Empty,
                ExpectedHarvestStart = entity.ExpectedHarvestStart,
                ActualYield = entity.ActualYield ?? 0,
                QualityGrade = entity.QualityGrade ?? "Chưa đánh giá",
                ExpectedHarvestEnd = entity.ExpectedHarvestEnd,
                EstimatedYield = entity.EstimatedYield,
                PlannedQuality = entity.PlannedQuality ?? string.Empty,
                Status = Enum.TryParse<CropDetailStatus>(entity.Status, true, out var parsedStatus)
                            ? parsedStatus : CropDetailStatus.Planned,

                FarmerId = entity.CropSeason?.FarmerId ?? Guid.Empty,
                FarmerName = entity.CropSeason?.Farmer?.User?.Name ?? "Không rõ"
            };
        }



        public static CropSeasonDetail MapToNewCropSeasonDetail(this CropSeasonDetailCreateDto dto)
        {
            return new CropSeasonDetail
            {
                DetailId = Guid.NewGuid(),
                CropSeasonId = dto.CropSeasonId,
                CoffeeTypeId = dto.CoffeeTypeId,
                ExpectedHarvestStart = dto.ExpectedHarvestStart,
                ExpectedHarvestEnd = dto.ExpectedHarvestEnd,
                EstimatedYield = dto.EstimatedYield,
                AreaAllocated = dto.AreaAllocated,
                PlannedQuality = dto.PlannedQuality,
                Status = dto.Status.ToString(),
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime()
            };
        }

        public static void MapToExistingEntity(this CropSeasonDetailUpdateDto dto, CropSeasonDetail entity)
        {
            entity.CoffeeTypeId = dto.CoffeeTypeId;
            entity.ExpectedHarvestStart = dto.ExpectedHarvestStart;
            entity.ExpectedHarvestEnd = dto.ExpectedHarvestEnd;
            entity.EstimatedYield = dto.EstimatedYield;
            entity.AreaAllocated = dto.AreaAllocated;
            entity.PlannedQuality = dto.PlannedQuality;
            entity.Status = dto.Status.ToString();
            entity.UpdatedAt = DateTime.Now;
        }
    }
}
