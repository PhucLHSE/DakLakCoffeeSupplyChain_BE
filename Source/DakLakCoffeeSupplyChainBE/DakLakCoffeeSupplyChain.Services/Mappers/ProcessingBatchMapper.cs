using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using DakLakCoffeeSupplyChain.Common.DTOs.EvaluationDTOs;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingBatchMapper
    {
        public static ProcessingBatchViewDto MapToProcessingBatchViewDto(this ProcessingBatch entity)
        {
            return new ProcessingBatchViewDto
            {
                BatchId = entity.BatchId,
                BatchCode = entity.BatchCode,
                SystemBatchCode = entity.SystemBatchCode,

                FarmerId = entity.FarmerId,
                FarmerName = entity.Farmer?.User?.Name ?? "N/A",

                MethodId = entity.MethodId,
                MethodName = entity.Method?.Name ?? "N/A",
                CropSeasonId = entity.CropSeasonId,
                CropSeasonName = entity.CropSeason?.SeasonName ?? "N/A",

                StageCount = entity.ProcessingBatchProgresses?.Count ?? 0,
                TotalInputQuantity = entity.InputQuantity,
                TotalOutputQuantity = 0,
                Status = Enum.TryParse<ProcessingStatus>(entity.Status, out var statusEnum)
                         ? statusEnum
                         : ProcessingStatus.NotStarted,
                CreatedAt = entity.CreatedAt ?? DateTime.MinValue
            };
        }
        public static ProcessingBatchDetailFullDto MapToFullDetailDto(
    this ProcessingBatch batch,
    string farmerName,
    string coffeeTypeName,
    string cropSeasonName,
    string methodName,
    List<ProcessingProgressWithStageDto> progresses
)
        {
            return new ProcessingBatchDetailFullDto
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                SystemBatchCode = batch.SystemBatchCode,
                Status = batch.Status,
                CreatedAt = batch.CreatedAt ?? DateTime.MinValue,

                FarmerId = batch.FarmerId,
                FarmerName = farmerName,
                CropSeasonId = batch.CropSeasonId,
                CoffeeTypeName = coffeeTypeName,
                CropSeasonName = cropSeasonName,
                MethodId = batch.MethodId,
                MethodName = methodName,

                TotalInputQuantity = batch.InputQuantity,
                Progresses = progresses
            };
        }

        public static CoffeeTypeViewAllDto MapToCoffeeTypeDto(this CoffeeType entity)
        {
            return new CoffeeTypeViewAllDto
            {
                CoffeeTypeId = entity.CoffeeTypeId,
                TypeName = entity.TypeName,
                Description = entity.Description
            };
        }
    }
}
