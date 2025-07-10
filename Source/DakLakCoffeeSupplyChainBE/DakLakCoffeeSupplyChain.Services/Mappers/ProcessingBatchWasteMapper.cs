using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
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
                RecordedBy = recordedByName,
                IsDisposed = waste.IsDisposed ?? false,
                DisposedAt = waste.DisposedAt,
                CreatedAt = waste.CreatedAt,
                UpdatedAt = waste.UpdatedAt
            };
        }
    }
}
