using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                StageCount = entity.ProcessingBatchProgresses?.Count ?? 0,
                TotalInputQuantity = entity.InputQuantity,
                TotalOutputQuantity = 0,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt ?? DateTime.MinValue
            };
        }
    }
}
