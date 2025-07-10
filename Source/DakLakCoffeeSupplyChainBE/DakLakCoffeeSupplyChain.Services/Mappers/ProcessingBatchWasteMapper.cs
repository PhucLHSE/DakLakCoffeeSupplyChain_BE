using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingBatchWasteMapper
    {
        public static ProcessingWasteViewAllDto MapToViewAllDto(this ProcessingBatchWaste entity, string recordedByName)
        {
            return new ProcessingWasteViewAllDto
            {
                WasteId = entity.WasteId,
                WasteCode = entity.WasteCode,
                ProgressId = entity.ProgressId,
                WasteType = entity.WasteType ?? string.Empty,
                Quantity = entity.Quantity ?? 0,
                Unit = entity.Unit ?? string.Empty,
                Note = entity.Note ?? string.Empty,
                RecordedAt = entity.RecordedAt,
                RecordedBy = recordedByName, 
                IsDisposed = entity.IsDisposed ?? false,
                DisposedAt = entity.DisposedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

       public static ProcessingWasteViewDetailsDto MapToDetailsDto(this ProcessingBatchWaste waste, string recordedByName)
{
    return new ProcessingWasteViewDetailsDto
    {
        WasteId = waste.WasteId,
        WasteCode = waste.WasteCode ?? string.Empty,
        ProgressId = waste.ProgressId,
        WasteType = waste.WasteType ?? string.Empty,
        Quantity = waste.Quantity ?? 0,
        Unit = waste.Unit ?? string.Empty,
        Note = waste.Note ?? string.Empty,
        RecordedAt = waste.RecordedAt,
        RecordedBy = recordedByName, // Passed in as an argument
        IsDisposed = waste.IsDisposed ?? false,
        DisposedAt = waste.DisposedAt,
        CreatedAt = waste.CreatedAt,
        UpdatedAt = waste.UpdatedAt
    };
}
        public static ProcessingBatchWaste MapToNewEntity(this ProcessingWasteCreateDto dto, string code, Guid userId)
        {
            return new ProcessingBatchWaste
            {
                WasteId = Guid.NewGuid(),
                WasteCode = code, // Code passed in as an argument (possibly auto-generated)
                ProgressId = dto.ProgressId,
                WasteType = dto.WasteType,
                Quantity = dto.Quantity,
                Unit = dto.Unit,
                Note = dto.Note,
                RecordedAt = dto.RecordedAt ?? DateTime.UtcNow,
                RecordedBy = userId, // Recorded by the user making the request (probably a farmer)
                IsDisposed = false, // New waste entries are not disposed initially
                CreatedAt = DateHelper.NowVietnamTime(), // Using a helper to get the current time
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

    }
}
