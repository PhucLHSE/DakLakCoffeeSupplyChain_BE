using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.InventoryLogEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseReceiptMapper
    {
        public static WarehouseReceipt ToEntityFromCreateDto(
            this WarehouseReceiptCreateDto dto,
            Guid receiptId,
            string receiptCode,
            Guid staffId,
            Guid batchId)
        {
            return new WarehouseReceipt
            {
                ReceiptId = receiptId,
                ReceiptCode = receiptCode,
                InboundRequestId = dto.InboundRequestId,
                WarehouseId = dto.WarehouseId,
                BatchId = batchId,
                ReceivedBy = staffId,
                ReceivedQuantity = dto.ReceivedQuantity,
                ReceivedAt = DateTime.UtcNow,
                Note = dto.Note,
                QrcodeUrl = "",
                IsDeleted = false
            };
        }

        public static InventoryLog ToInventoryLogFromInbound(
            this WarehouseReceipt receipt,
            Guid inventoryId,
            double confirmedQuantity,
            string? confirmNote = null)
        {
            var logNote = $"Nhập kho từ phiếu {receipt.ReceiptCode}";

            if (confirmNote != null)
                logNote += $" | {confirmNote}";

            return new InventoryLog
            {
                LogId = Guid.NewGuid(),
                InventoryId = inventoryId,
                ActionType = InventoryLogActionType.increase.ToString(),
                QuantityChanged = confirmedQuantity,
                Note = logNote,
                TriggeredBySystem = true,
                LoggedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static string BuildConfirmationNote(double original, double confirmed, string? reason)
        {
            var noteParts = new List<string>();

            if (original != confirmed)
                noteParts.Add($"[Chênh lệch: tạo {original}kg, xác nhận {confirmed}kg]");

            if (!string.IsNullOrWhiteSpace(reason))
                noteParts.Add($"[Lý do: {reason}]");

            noteParts.Add($"[Confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm}]");

            return string.Join(" ", noteParts);
        }

        public static Inventory ToNewInventory(
            Guid warehouseId,
            Guid batchId,
            double quantity,
            string inventoryCode)
        {
            return new Inventory
            {
                InventoryId = Guid.NewGuid(),
                InventoryCode = inventoryCode,
                WarehouseId = warehouseId,
                BatchId = batchId,
                Quantity = quantity,
                Unit = "kg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }
    }
}
