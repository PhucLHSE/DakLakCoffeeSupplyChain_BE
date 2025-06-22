using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs
{
    public class WarehouseOutboundReceiptConfirmDto
    {
        public double ConfirmedQuantity { get; set; }
        public string? DestinationNote { get; set; }
    }
}
