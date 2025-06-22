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
    }
}
