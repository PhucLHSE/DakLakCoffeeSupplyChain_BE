using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingBatchProgressMapper
    {
        public static ProcessingBatchProgressViewAllDto MapToProcessingBatchProgressViewAllDto(this ProcessingBatchProgress entity)
        {
            return new ProcessingBatchProgressViewAllDto
            {
                ProgressId = entity.ProgressId,
                BatchId = entity.BatchId,
                BatchCode = entity.Batch?.BatchCode ?? "N/A",
                StepIndex = entity.StepIndex,
                StageId = entity.StageId,
                StageName = entity.Stage?.StageName ?? "N/A",
                StageDescription = entity.StageDescription,
                ProgressDate = entity.ProgressDate,
                OutputQuantity = entity.OutputQuantity,
                OutputUnit = entity.OutputUnit,
                PhotoUrl = entity.PhotoUrl,
                VideoUrl = entity.VideoUrl,
                UpdatedByName = entity.UpdatedByNavigation?.User?.Name ?? "N/A",
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
