using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs
{
    public class WarehouseOutboundReceiptCreateDto
    {
        public Guid OutboundRequestId { get; set; }
        public double ExportedQuantity { get; set; }
        public string Note { get; set; }
    }
}
