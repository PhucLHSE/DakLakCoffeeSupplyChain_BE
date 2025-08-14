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
            public Guid WarehouseId { get; set; }       // thêm
            public Guid InventoryId { get; set; }       // thêm
            public double ExportedQuantity { get; set; }
            public string Note { get; set; }
            public string Destination { get; set; }     // thêm field này
        }
    }

