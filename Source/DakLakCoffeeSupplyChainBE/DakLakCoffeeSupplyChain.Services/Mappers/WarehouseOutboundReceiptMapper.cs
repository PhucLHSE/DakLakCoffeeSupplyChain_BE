using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.InventoryLogEnums;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseOutboundReceiptMapper
    {
        public static WarehouseOutboundReceipt MapFromCreateDto(
            this WarehouseOutboundReceiptCreateDto dto,
            Guid outboundReceiptId,
            string receiptCode,
            Guid staffId,
            Guid batchId)
        {
            return new WarehouseOutboundReceipt
            {
                OutboundReceiptId = outboundReceiptId,
                OutboundReceiptCode = receiptCode,
                OutboundRequestId = dto.OutboundRequestId,
                WarehouseId = dto.WarehouseId,
                InventoryId = dto.InventoryId,
                BatchId = batchId,
                Quantity = dto.ExportedQuantity,                 // SL ghi nhận cho phiếu (draft)
                ExportedBy = staffId,
                ExportedAt = DateTime.UtcNow,
                Note = dto.Note,                                  // Confirm sẽ append "[CONFIRMED:x]"
                DestinationNote = dto.Destination ?? "",          // Map từ Destination
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        // LƯU Ý: Không ghi đè Quantity nữa để hỗ trợ partial; chỉ append tag xác nhận
        public static void UpdateAfterConfirm(
            this WarehouseOutboundReceipt receipt,
            double confirmedQuantity,
            string? destinationNote)
        {
            // Append tag xác nhận cho lần này
            receipt.Note = (receipt.Note ?? "") + $" [CONFIRMED:{confirmedQuantity}]";
            if (!string.IsNullOrWhiteSpace(destinationNote))
                receipt.DestinationNote = destinationNote!;
            receipt.UpdatedAt = DateTime.UtcNow;
        }

        public static void MarkAsCompleted(this WarehouseOutboundRequest request)
        {
            request.Status = WarehouseOutboundRequestStatus.Completed.ToString();
            request.UpdatedAt = DateTime.UtcNow;
        }

        public static InventoryLog ToInventoryLogFromOutbound(
            this WarehouseOutboundReceipt receipt,
            Guid inventoryId,
            double confirmedQuantity)
        {
            return new InventoryLog
            {
                LogId = Guid.NewGuid(),
                InventoryId = inventoryId,
                ActionType = InventoryLogActionType.decrease.ToString(),
                QuantityChanged = -confirmedQuantity,
                Note = $"Xuất kho từ phiếu {receipt.OutboundReceiptCode}",
                TriggeredBySystem = true,
                LoggedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static WarehouseOutboundReceiptListItemDto ToListItemDto(this WarehouseOutboundReceipt r)
        {
            return new WarehouseOutboundReceiptListItemDto
            {
                OutboundReceiptId = r.OutboundReceiptId,
                OutboundReceiptCode = r.OutboundReceiptCode,
                WarehouseName = r.Warehouse?.Name ?? "N/A",
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                Quantity = r.Quantity,
                ExportedAt = r.ExportedAt,
                StaffName = r.ExportedByNavigation?.User?.Name ?? "N/A"
            };
        }

        public static WarehouseOutboundReceiptDetailDto ToDetailDto(this WarehouseOutboundReceipt r)
        {
            return new WarehouseOutboundReceiptDetailDto
            {
                OutboundReceiptId = r.OutboundReceiptId,
                OutboundReceiptCode = r.OutboundReceiptCode,
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? "N/A",
                BatchId = r.BatchId,
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                Quantity = r.Quantity,
                ExportedAt = r.ExportedAt,
                StaffName = r.ExportedByNavigation?.User?.Name ?? "N/A",
                Note = r.Note,
                DestinationNote = r.DestinationNote
            };
        }
    }
}
