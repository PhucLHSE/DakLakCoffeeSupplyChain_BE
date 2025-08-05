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
            var planDetail = entity.CommitmentDetail?.PlanDetail;
            var coffeeType = planDetail?.CoffeeType;

            return new CropSeasonDetailViewDto
            {
                DetailId = entity.DetailId,
                AreaAllocated = entity.AreaAllocated ?? 0,
                CoffeeTypeId = planDetail?.CoffeeTypeId ?? Guid.Empty,
                TypeName = coffeeType?.TypeName ?? "Không rõ",
                CommitmentDetailId = entity.CommitmentDetailId,
                CommitmentDetailCode = entity.CommitmentDetail?.CommitmentDetailCode ?? "",
                ExpectedHarvestStart = entity.ExpectedHarvestStart,
                ExpectedHarvestEnd = entity.ExpectedHarvestEnd,
                EstimatedYield = entity.EstimatedYield,
                ActualYield = entity.ActualYield ?? 0,
                PlannedQuality = entity.PlannedQuality ?? string.Empty,
                QualityGrade = entity.QualityGrade ?? "Chưa đánh giá",
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
                CommitmentDetailId = dto.CommitmentDetailId,
                ExpectedHarvestStart = dto.ExpectedHarvestStart,
                ExpectedHarvestEnd = dto.ExpectedHarvestEnd,
                AreaAllocated = dto.AreaAllocated ?? 0,
                EstimatedYield = 0, 
                PlannedQuality = dto.PlannedQuality,
                Status = CropDetailStatus.Planned.ToString(),
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime()
            };
        }


        public static void MapToExistingEntity(this CropSeasonDetailUpdateDto dto, CropSeasonDetail entity)
        {
            entity.ExpectedHarvestStart = dto.ExpectedHarvestStart;
            entity.ExpectedHarvestEnd = dto.ExpectedHarvestEnd;
            entity.AreaAllocated = dto.AreaAllocated ?? 0;
            entity.PlannedQuality = dto.PlannedQuality;
            entity.UpdatedAt = DateHelper.NowVietnamTime(); 
        }
    }
    }
