using DakLakCoffeeSupplyChain.Common.DTOs.InventoryLogDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class InventoryLogMapper
    {
        public static InventoryLogListItemDto ToListItemDto(this InventoryLog log, string? updatedByName)
        {
            return new InventoryLogListItemDto
            {
                LogId = log.LogId,
                ActionType = log.ActionType,
                QuantityChanged = log.QuantityChanged,
                Note = log.Note,
                LoggedAt = log.LoggedAt,
                TriggeredBySystem = log.TriggeredBySystem ?? false,
                UpdatedByName = updatedByName ?? "Không rõ"
            };
        }
    }
}
