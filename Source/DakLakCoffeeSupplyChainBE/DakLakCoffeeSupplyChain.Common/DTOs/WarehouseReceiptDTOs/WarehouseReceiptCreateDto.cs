using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs
{
    public class WarehouseReceiptCreateDto
    {
        public Guid InboundRequestId { get; set; }
        public Guid WarehouseId { get; set; }
        public double ReceivedQuantity { get; set; }
        public string? Note { get; set; }
    }
}
