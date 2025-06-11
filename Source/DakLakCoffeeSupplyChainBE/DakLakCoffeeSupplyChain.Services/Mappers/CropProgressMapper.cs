using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropProgressMapper
    {
        public static CropProgressViewAllDto ToViewAllDto(this CropProgress entity)
        {
            return new CropProgressViewAllDto
            {
                ProgressId = entity.ProgressId,
                StageName = entity.Stage?.StageName ?? string.Empty,
                ProgressDate = entity.ProgressDate,
                Note = entity.Note ?? string.Empty,
                PhotoUrl = entity.PhotoUrl ?? string.Empty,
                VideoUrl = entity.VideoUrl ?? string.Empty
            };
        }

        public static CropProgressViewDetailsDto ToViewDetailsDto(this CropProgress entity)
        {
            return new CropProgressViewDetailsDto
            {
                ProgressId = entity.ProgressId,
                CropSeasonDetailId = entity.CropSeasonDetailId,
                StageId = entity.StageId,
                StageName = entity.Stage?.StageName ?? string.Empty,
                StageDescription = entity.StageDescription ?? string.Empty,
                ProgressDate = entity.ProgressDate,
                Note = entity.Note ?? string.Empty,
                PhotoUrl = entity.PhotoUrl ?? string.Empty,
                VideoUrl = entity.VideoUrl ?? string.Empty,
                UpdatedByName = entity.UpdatedByNavigation?.User?.Name ?? string.Empty,
                StepIndex = entity.StepIndex,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
