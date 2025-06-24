using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingParameterMapper
    {
        public static ProcessingParameterViewAllDto MapToProcessingParameterViewAllDto(this ProcessingParameter entity)
        {
            return new ProcessingParameterViewAllDto
            {
                ParameterId = entity.ParameterId,
                ProgressId = entity.ProgressId,
                ParameterName = entity.ParameterName,
                ParameterValue = entity.ParameterValue,
                Unit = entity.Unit,
                RecordedAt = entity.RecordedAt
            };
        }
        public static ProcessingParameterViewDetailDto MapToProcessingParameterDetailDto(this ProcessingParameter entity)
        {
            return new ProcessingParameterViewDetailDto
            {
                ParameterId = entity.ParameterId,
                ProgressId = entity.ProgressId,
                ParameterName = entity.ParameterName,
                ParameterValue = entity.ParameterValue,
                Unit = entity.Unit,
                RecordedAt = entity.RecordedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
