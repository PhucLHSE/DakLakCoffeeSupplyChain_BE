using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDto;
using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;


namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropStageMapper
    {
        public static CropStageViewAllDto ToViewDto(this CropStage entity)
        {
            return new CropStageViewAllDto
            {
                StageId = entity.StageId,
                StageCode = entity.StageCode ?? string.Empty,
                StageName = entity.StageName ?? string.Empty,
                Description = entity.Description,
                OrderIndex = entity.OrderIndex
            };
        }
        public static CropStageViewDto MapToViewDto(this CropStage entity)
        {
            return new CropStageViewDto
            {
                StageId = entity.StageId,
                StageName = entity.StageName,
                Description = entity.Description ?? string.Empty,
                Order = entity.OrderIndex ?? 0,
                CreatedAt = entity.CreatedAt
            };
        }

        public static CropStage MapToNewCropStage(this CropStageCreateDto dto)
        {
            return new CropStage
            {
                StageCode = dto.StageCode,
                StageName = dto.StageName,
                Description = dto.Description,
                OrderIndex = dto.OrderIndex,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        public static void MapToUpdateCropStage(this CropStageUpdateDto dto, CropStage entity)
        {
            entity.StageCode = dto.StageCode;
            entity.StageName = dto.StageName;
            entity.Description = dto.Description;
            entity.OrderIndex = dto.OrderIndex;
            entity.UpdatedAt = DateHelper.NowVietnamTime();
        }

    }
}
