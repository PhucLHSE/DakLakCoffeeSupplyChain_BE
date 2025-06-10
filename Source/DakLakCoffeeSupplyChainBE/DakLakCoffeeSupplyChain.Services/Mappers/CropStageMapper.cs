using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDto;
using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs;
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

    }
}
