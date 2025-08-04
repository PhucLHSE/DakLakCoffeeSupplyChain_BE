using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
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
        public static ProcessingBatchDetailsDto MapToDetailsDto(this ProcessingBatch batch, string farmerName)
        {
            return new ProcessingBatchDetailsDto
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                SystemBatchCode = batch.SystemBatchCode,
                CropSeasonId = batch.CropSeasonId,
                CropSeasonName = batch.CropSeason?.SeasonName ?? "Unknown",
                FarmerId = batch.FarmerId,
                FarmerName = batch.Farmer?.User?.Name ?? "Unknown",
                MethodId = batch.MethodId,
                MethodName = batch.Method?.Name ?? "Unknown",
                InputQuantity = batch.InputQuantity,
                InputUnit = batch.InputUnit,
                Status = Enum.TryParse<ProcessingStatus>(batch.Status, out var statusEnum)
                    ? statusEnum
                    : ProcessingStatus.NotStarted,
                CreatedAt = batch.CreatedAt ?? DateTime.MinValue,
                UpdatedAt = batch.UpdatedAt,
                Products = batch.Products?
                    .Where(p => !p.IsDeleted)
                    .Select(p =>
                    {
                        var unitEnum = Enum.TryParse<ProductUnit>(p.Unit, ignoreCase: true, out var parsedUnit)
                            ? parsedUnit
                            : ProductUnit.Kg;

                        return new ProductViewDetailsDto
                        {
                            ProductId = p.ProductId,
                            ProductCode = p.ProductCode,
                            ProductName = p.ProductName ?? "",
                            CoffeeTypeName = p.CoffeeType?.TypeName ?? "",
                            Unit = unitEnum, // enum thay vì string
                            QuantityAvailable = p.QuantityAvailable,
                            UnitPrice = p.UnitPrice
                        };
                    }).ToList() ?? new(),
                Progresses = batch.ProcessingBatchProgresses?
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.StepIndex)
                    .Select(p => new ProcessingBatchProgressDetailDto
                    {
                        ProgressId = p.ProgressId,
                        BatchId = p.BatchId,
                        BatchCode = batch.BatchCode,
                        StepIndex = p.StepIndex,
                        StageId = p.StageId,
                        StageName = p.Stage?.StageName ?? "",
                        StageDescription = p.Stage?.Description ?? "",
                        ProgressDate = p.ProgressDate,
                        OutputQuantity = p.OutputQuantity,
                        OutputUnit = p.OutputUnit,
                        PhotoUrl = p.PhotoUrl,
                        VideoUrl = p.VideoUrl,
                        UpdatedAt = p.UpdatedAt,
                        UpdatedByName = farmerName,
                        Parameters = p.ProcessingParameters?.Select(param => new ProcessingParameterViewAllDto
                        {
                            ParameterId = param.ParameterId,
                            ProgressId = param.ProgressId,
                            ParameterName = param.ParameterName ?? "",
                            ParameterValue = param.ParameterValue ?? "",
                            Unit = param.Unit ?? "",
                            RecordedAt = param.RecordedAt
                        }).ToList() ?? new()
                    }).ToList() ?? new()
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
