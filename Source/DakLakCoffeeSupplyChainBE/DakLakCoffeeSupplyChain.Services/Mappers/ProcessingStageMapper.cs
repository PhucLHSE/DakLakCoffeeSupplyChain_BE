using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingStageMapper
    {
        public static ProcessingStageViewAllDto MapToProcessingStageViewAllDto(this ProcessingStage stage)
        {
            return new ProcessingStageViewAllDto
            {
                StageId = stage.StageId,
                StageCode = stage.StageCode,
                StageName = stage.StageName,
                Description = stage.Description,
                OrderIndex = stage.OrderIndex,
                IsRequired = stage.IsRequired ?? true,

                MethodId = stage.MethodId,
                MethodCode = stage.Method?.MethodCode ?? string.Empty,
                MethodName = stage.Method?.Name ?? string.Empty
            };
        }
        public static ProcessingStageViewDetailDto MapToProcessingStageViewDetailDto(this ProcessingStage entity)
        {
            return new ProcessingStageViewDetailDto
            {
                StageId = entity.StageId,
                StageCode = entity.StageCode,
                StageName = entity.StageName,
                Description = entity.Description,
                OrderIndex = entity.OrderIndex,
                IsRequired = entity.IsRequired ?? false,
                MethodId = entity.MethodId,
                MethodName = entity.Method?.Name ?? string.Empty,
                MethodCode = entity.Method?.MethodCode ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsDeleted = entity.IsDeleted
            };
        }
        public static ProcessingStage MapToProcessingStageCreateEntity(this CreateProcessingStageDto dto)
        {
            return new ProcessingStage
            {
                StageCode = dto.StageCode,
                StageName = dto.StageName,
                Description = dto.Description,
                OrderIndex = dto.OrderIndex,
                IsRequired = dto.IsRequired,
                MethodId = dto.MethodId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }
    }
}
