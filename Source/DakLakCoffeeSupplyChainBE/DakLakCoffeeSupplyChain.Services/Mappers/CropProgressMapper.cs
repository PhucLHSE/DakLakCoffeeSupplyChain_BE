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
                StageId = entity.StageId,
                StepIndex = entity.StepIndex,
                StageCode = entity.Stage?.StageCode ?? string.Empty,
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
                StageCode = entity.Stage?.StageCode ?? string.Empty,
                StageDescription = entity.Stage?.Description ?? string.Empty, // Sửa: lấy từ Stage.Description (không phải StageDescription)
                ProgressDate = entity.ProgressDate,
                Note = entity.Note ?? string.Empty,
                PhotoUrl = entity.PhotoUrl ?? string.Empty,
                VideoUrl = entity.VideoUrl ?? string.Empty,
                UpdatedByName = entity.UpdatedByNavigation?.User?.Name ?? string.Empty,
                UpdatedBy = entity.UpdatedBy,
                StepIndex = entity.Stage?.OrderIndex ?? entity.StageId, // Sửa: tự động lấy từ Stage.OrderIndex
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ActualYield = entity.CropSeasonDetail?.ActualYield
            };
        }


        public static CropProgress MapToCropProgressCreateDto(this CropProgressCreateDto dto)
        {
            var now = DateHelper.NowVietnamTime();

            return new CropProgress
            {
                ProgressId = Guid.NewGuid(),
                CropSeasonDetailId = dto.CropSeasonDetailId,
                StageId = dto.StageId,
                // StageDescription sẽ được set tự động từ CropStage.Description trong service
                ProgressDate = dto.ProgressDate, 
                PhotoUrl = dto.PhotoUrl ?? string.Empty,
                VideoUrl = dto.VideoUrl ?? string.Empty,
                Note = dto.Note ?? string.Empty,
                StepIndex = dto.StageId, // Sửa: tự động dùng StageId làm StepIndex (sẽ được cập nhật sau)
                CreatedAt = now,
                UpdatedAt = now,
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