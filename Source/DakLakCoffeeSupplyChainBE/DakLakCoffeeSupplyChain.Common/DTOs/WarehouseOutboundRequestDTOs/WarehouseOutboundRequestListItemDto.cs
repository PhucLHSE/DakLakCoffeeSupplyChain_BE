﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs
{
    public class WarehouseOutboundRequestListItemDto
    {
        public Guid OutboundRequestId { get; set; }
        public string OutboundRequestCode { get; set; }
        public string Status { get; set; }

        public Guid WarehouseId { get; set; }         // ✅ thêm vào
        public string? WarehouseName { get; set; }

        public Guid InventoryId { get; set; }         // ✅ thêm vào
        public double RequestedQuantity { get; set; }
        public string Unit { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
