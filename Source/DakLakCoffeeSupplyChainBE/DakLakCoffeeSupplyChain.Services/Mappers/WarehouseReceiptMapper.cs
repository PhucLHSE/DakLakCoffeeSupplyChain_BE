using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.InventoryLogEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseReceiptMapper
    {
        public static WarehouseReceipt ToEntityFromCreateDto(
            this WarehouseReceiptCreateDto dto,
            Guid receiptId,
            string receiptCode,
            Guid staffId,
            Guid? batchId = null,
            Guid? detailId = null)
        {
            return new WarehouseReceipt
            {
                ReceiptId = receiptId,
                ReceiptCode = receiptCode,
                InboundRequestId = dto.InboundRequestId,
                WarehouseId = dto.WarehouseId,
                BatchId = batchId,
                DetailId = detailId,  // Thêm DetailId cho cà phê tươi
                ReceivedBy = staffId,
                LotCode = "", // Có thể để trống
                ReceivedQuantity = null, // Chỉ set khi xác nhận
                ReceivedAt = null, // Chỉ set khi xác nhận
                Note = "", // Chỉ set khi xác nhận
                QrcodeUrl = "", // Có thể để trống
                IsDeleted = false // Mặc định không bị xóa
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
                UpdatedBy = receipt.ReceivedBy, // ✅ Track staff thực hiện nhập kho
                TriggeredBySystem = false, // ✅ Không phải hệ thống tự động
                LoggedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static string BuildConfirmationNote(double requested, double confirmed, string? reason)
        {
            var noteParts = new List<string>();

            if (requested != confirmed)
                noteParts.Add($"[Chênh lệch: yêu cầu {requested}kg, xác nhận {confirmed}kg]");

            if (!string.IsNullOrWhiteSpace(reason))
                noteParts.Add($"[Lý do: {reason}]");

            noteParts.Add($"[Confirmed at {DateTime.UtcNow:yyyy-MM-dd HH:mm}]");

            return string.Join(" ", noteParts);
        }

        public static Inventory ToNewInventory(
            Guid warehouseId,
            Guid? batchId = null,
            Guid? detailId = null,
            double quantity = 0,
            string inventoryCode = "")
        {
            return new Inventory
            {
                InventoryId = Guid.NewGuid(),
                InventoryCode = inventoryCode,
                WarehouseId = warehouseId,
                BatchId = batchId,
                DetailId = detailId,  // Thêm DetailId cho cà phê tươi
                Quantity = quantity,
                Unit = "kg",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static WarehouseReceiptListItemDto ToListItemDto(this WarehouseReceipt receipt)
        {
            return new WarehouseReceiptListItemDto
            {
                ReceiptId = receipt.ReceiptId,
                ReceiptCode = receipt.ReceiptCode,
                WarehouseName = receipt.Warehouse?.Name,
                
                // Thông tin cho cà phê sơ chế
                BatchId = receipt.BatchId,
                BatchCode = receipt.Batch?.BatchCode,
                
                // Thông tin cho cà phê tươi
                DetailId = receipt.DetailId,
                DetailCode = receipt.Detail?.CropSeason?.SeasonName,
                CoffeeType = receipt.BatchId != null 
                    ? receipt.Batch?.CoffeeType?.TypeName ?? "N/A"
                    : receipt.Detail?.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "N/A",
                CropSeasonName = receipt.Detail?.CropSeason?.SeasonName,
                
                ReceivedQuantity = receipt.ReceivedQuantity,
                ReceivedAt = receipt.ReceivedAt,
                StaffName = receipt.ReceivedByNavigation?.User?.Name,
                Note = receipt.Note
            };
        }
    }
}
