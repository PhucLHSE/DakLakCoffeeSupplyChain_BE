using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CropMapper
    {
        // Mapper Crop -> CropViewAllDto
        public static CropViewAllDto MapToCropViewAllDto(this Crop crop)
        {
            // Parse string to enum
            CropStatus status = Enum.TryParse<CropStatus>(crop.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : CropStatus.Other;

            return new CropViewAllDto
            {
                CropId = crop.CropId,
                CropCode = crop.CropCode ?? string.Empty,
                FarmName = crop.FarmName ?? string.Empty,
                CropArea = crop.CropArea,
                Status = status
            };
        }
    }
}
