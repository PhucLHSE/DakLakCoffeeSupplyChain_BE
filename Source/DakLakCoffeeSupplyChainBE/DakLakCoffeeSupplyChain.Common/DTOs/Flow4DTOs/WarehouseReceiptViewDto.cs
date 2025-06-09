using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs
{
    public class WarehouseReceiptViewDto
    {
        public Guid ReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public string WarehouseName { get; set; }
        public double? ReceivedQuantity { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? StaffName { get; set; }
    }
}
