using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.Flow3DTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingMethodMapper
    {
        public static ProcessingMethodViewAllDto MapToProcessingMethodViewAllDto(this ProcessingMethod method)
        {
            return new ProcessingMethodViewAllDto
            {
                MethodId = method.MethodId,
                MethodCode = method.MethodCode,
                Name = method.Name
            };
        }

        public static ProcessingMethodDetailDto MapToProcessingMethodDetailDto(this ProcessingMethod method)
        {
            return new ProcessingMethodDetailDto
            {
                MethodId = method.MethodId,
                MethodCode = method.MethodCode,
                Name = method.Name,
                Description = method.Description,
                StageCount = method.ProcessingStages?.Count ?? 0
            };
        }
    }
}
