﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs
{
    public class OrderItemViewDto
    {
        public Guid OrderItemId { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public double? Quantity { get; set; }

        public double? UnitPrice { get; set; }

        public double? DiscountAmount { get; set; }

        public double? TotalPrice { get; set; }

        public string Note { get; set; } = string.Empty;
    }
}
