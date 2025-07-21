using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingBatchProgressMapper
    {
        public static ProcessingBatchProgressViewAllDto MapToProcessingBatchProgressViewAllDto(
       this ProcessingBatchProgress entity,
       ProcessingBatch batch)
        {
            return new ProcessingBatchProgressViewAllDto
            {
                ProgressId = entity.ProgressId,
                BatchId = entity.BatchId,
                BatchCode = batch?.BatchCode ?? "N/A",
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

        public static ProcessingBatchProgressDetailDto MapToProcessingBatchProgressDetailDto(this ProcessingBatchProgress entity)
        {
            return new ProcessingBatchProgressDetailDto
            {
                ProgressId = entity.ProgressId,
                BatchId = entity.BatchId,
                BatchCode = entity.Batch?.BatchCode,
                StepIndex = entity.StepIndex,
                StageId = entity.StageId,
                StageName = entity.Stage?.StageName,
                StageDescription = entity.StageDescription,
                ProgressDate = entity.ProgressDate,
                OutputQuantity = entity.OutputQuantity,
                OutputUnit = entity.OutputUnit,
                PhotoUrl = entity.PhotoUrl,
                VideoUrl = entity.VideoUrl,
                UpdatedByName = entity.UpdatedByNavigation?.User?.Name,
                UpdatedAt = entity.UpdatedAt,
                Parameters = entity.ProcessingParameters?
                .Where(p => !p.IsDeleted)
                .Select(p => new ProcessingParameterViewAllDto
                {
                    ParameterId = p.ParameterId,
                    ProgressId = p.ProgressId,
                    ParameterName = p.ParameterName,
                    ParameterValue = p.ParameterValue,
                    Unit = p.Unit,
                    RecordedAt = p.RecordedAt
                }).ToList()
            };
        }
    }
}
