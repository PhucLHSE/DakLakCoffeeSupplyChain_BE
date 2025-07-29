using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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
                CropSeasonDetailId = entity.CropSeasonDetailId, 
                StageName = entity.Stage?.StageName ?? string.Empty,
                ProgressDate = entity.ProgressDate,
                Note = entity.Note ?? string.Empty,
                PhotoUrl = entity.PhotoUrl ?? string.Empty,
                VideoUrl = entity.VideoUrl ?? string.Empty
            };
        }


        public static CropProgressViewDetailsDto MapToCropProgressViewDetailsDto(this CropProgress entity)
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
                UpdatedBy = entity.UpdatedBy,
                StepIndex = entity.StepIndex,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }


        public static CropProgress MapToCropProgressCreateDto(this CropProgressCreateDto dto)
        {
            return new CropProgress
            {
                ProgressId = Guid.NewGuid(),
                CropSeasonDetailId = dto.CropSeasonDetailId,

                StageId = dto.StageId,
                StageDescription = dto.StageDescription ?? string.Empty,
                ProgressDate = dto.ProgressDate,
                PhotoUrl = dto.PhotoUrl ?? string.Empty,
                VideoUrl = dto.VideoUrl ?? string.Empty,
                Note = dto.Note ?? string.Empty,
                StepIndex = dto.StepIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }
        public static void MapToUpdateCropProgress(this CropProgressUpdateDto dto, CropProgress entity, Guid farmerId)
        {
            entity.CropSeasonDetailId = dto.CropSeasonDetailId;
            entity.UpdatedBy = farmerId; 
            entity.StageId = dto.StageId;
            entity.StageDescription = dto.StageDescription;
            entity.ProgressDate = dto.ProgressDate;
            entity.PhotoUrl = dto.PhotoUrl ?? string.Empty;
            entity.VideoUrl = dto.VideoUrl ?? string.Empty;
            entity.Note = dto.Note ?? string.Empty;
            entity.StepIndex = dto.StepIndex;
            entity.UpdatedAt = DateHelper.NowVietnamTime();
        }



    }
}
