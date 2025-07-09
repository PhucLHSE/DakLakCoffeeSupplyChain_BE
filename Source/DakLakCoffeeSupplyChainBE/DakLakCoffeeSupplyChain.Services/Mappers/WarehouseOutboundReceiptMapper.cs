using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseOutboundReceiptMapper
    {
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
