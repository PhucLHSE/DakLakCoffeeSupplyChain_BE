using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs
{
    public class WarehouseReceiptCreateDto
    {
        public Guid InboundRequestId { get; set; }
        public Guid WarehouseId { get; set; } // 👈 Thêm dòng này
        public double ReceivedQuantity { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string Note { get; set; }
    }
}
