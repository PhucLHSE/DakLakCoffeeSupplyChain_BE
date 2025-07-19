using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
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
                Quantity = dto.ExportedQuantity,
                ExportedBy = staffId,
                ExportedAt = DateTime.UtcNow,
                Note = dto.Note,
                DestinationNote = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static void UpdateAfterConfirm(
            this WarehouseOutboundReceipt receipt,
            double confirmedQuantity,
            string? destinationNote)
        {
            receipt.Quantity = confirmedQuantity;
            receipt.DestinationNote = destinationNote ?? "";
            receipt.Note = (receipt.Note ?? "") + $" [Đã xác nhận lúc {DateTime.UtcNow:HH:mm dd/MM/yyyy}]";
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
                ActionType = "ConfirmOutbound",
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
