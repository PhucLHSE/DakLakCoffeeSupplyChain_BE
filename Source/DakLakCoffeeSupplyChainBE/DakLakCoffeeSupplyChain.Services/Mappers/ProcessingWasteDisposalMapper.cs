using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ProcessingWasteDisposalMapper
    {
        public static ProcessingWasteDisposalViewAllDto MapToDto(this ProcessingWasteDisposal entity, string handledByName)
        {
            return new ProcessingWasteDisposalViewAllDto
            {
                DisposalId = entity.DisposalId,
                DisposalCode = entity.DisposalCode,
                WasteId = entity.WasteId,
                WasteName = entity.Waste?.WasteType ?? "N/A",
                DisposalMethod = entity.DisposalMethod,
                HandledBy = entity.HandledBy ?? Guid.Empty,
                HandledByName = handledByName,
                HandledAt = entity.HandledAt,
                Notes = entity.Notes,
                IsSold = entity.IsSold ?? false,
                Revenue = entity.Revenue,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
