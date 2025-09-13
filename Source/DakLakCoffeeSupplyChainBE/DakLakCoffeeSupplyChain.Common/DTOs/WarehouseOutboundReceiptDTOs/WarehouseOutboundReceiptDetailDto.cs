using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs
{
    public class WarehouseOutboundReceiptDetailDto
    {
        public Guid OutboundReceiptId { get; set; }
        public string OutboundReceiptCode { get; set; } = default!;
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid? BatchId { get; set; }
        public string? BatchCode { get; set; }
        public double Quantity { get; set; }
        public DateTime? ExportedAt { get; set; }
        public string? StaffName { get; set; }
        public string? Note { get; set; }
        public string? DestinationNote { get; set; }
        
        // Thông tin sản phẩm
        public string? InventoryName { get; set; }
        public string? CoffeeType { get; set; }
        public string? Quality { get; set; }
        public string? Origin { get; set; }
        public DateTime? ProductionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public double? MoistureContent { get; set; }
        public double? NetWeight { get; set; }
        
        // Thông tin đơn hàng liên kết
        public object? OrderInfo { get; set; }
        
        // Thông tin người tạo
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Chữ ký tự động
        public string? InspectorSignature { get; set; }
        public string? StaffSignature { get; set; }
        public string? RecipientSignature { get; set; }
    }
}
