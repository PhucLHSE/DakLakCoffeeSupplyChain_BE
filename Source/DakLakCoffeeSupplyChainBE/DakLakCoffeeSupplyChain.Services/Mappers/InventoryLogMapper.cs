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
                UpdatedByName = updatedByName ?? "Không rõ",
                InventoryCode = log.Inventory?.InventoryCode ?? "N/A",
                CoffeeTypeName = log.Inventory?.Batch?.CoffeeType?.TypeName ?? "N/A",
                WarehouseName = log.Inventory?.Warehouse?.Name ?? "N/A"
            };
        }
        public static InventoryLogByInventoryDto ToByInventoryDto(this InventoryLog log, string updatedByName)
        {
            return new InventoryLogByInventoryDto
            {
                LogId = log.LogId,
                ActionType = log.ActionType,
                QuantityChanged = log.QuantityChanged,
                Note = log.Note,
                LoggedAt = log.LoggedAt,
                TriggeredBySystem = log.TriggeredBySystem ?? false,
                UpdatedByName = updatedByName ?? "Không rõ",
                InventoryCode = log.Inventory?.InventoryCode ?? "N/A",
                WarehouseName = log.Inventory?.Warehouse?.Name ?? "N/A",
                BatchCode = log.Inventory?.Batch?.BatchCode ?? "N/A",
                ProductName = log.Inventory?.Batch?.Products?.FirstOrDefault()?.ProductName ?? "N/A",
                CoffeeTypeName = log.Inventory?.Batch?.CoffeeType?.TypeName ?? "N/A",
                SeasonCode = log.Inventory?.Batch?.CropSeason?.CropSeasonCode ?? "N/A",
                FarmerName = log.Inventory?.Batch?.Farmer?.User?.Name ?? "N/A"
            };
        }
    }
}
