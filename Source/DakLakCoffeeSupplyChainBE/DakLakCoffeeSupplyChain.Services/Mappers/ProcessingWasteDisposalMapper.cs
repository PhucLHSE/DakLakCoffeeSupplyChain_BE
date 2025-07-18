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
        public static ProcessingWasteDisposalViewAllDto MapToViewAllDto(
            this ProcessingWasteDisposal entity,
            string handledByName 
        )
        {
            return new ProcessingWasteDisposalViewAllDto
            {
                DisposalId = entity.DisposalId,
                DisposalCode = entity.DisposalCode ?? string.Empty,
                WasteId = entity.WasteId,
                DisposalMethod = entity.DisposalMethod ?? string.Empty,
                Notes = entity.Notes,
                IsSold = entity.IsSold ?? false,
                Revenue = entity.Revenue,
                HandledAt = entity.HandledAt,
                HandledByName = handledByName,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
