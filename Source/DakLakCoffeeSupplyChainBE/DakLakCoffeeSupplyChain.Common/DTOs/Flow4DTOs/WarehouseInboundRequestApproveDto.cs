using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs
{
    public class WarehouseInboundRequestApproveDto
    {
        public Guid InboundRequestId { get; set; }
        public Guid WarehouseId { get; set; }  // ✅ kho do FE chọn
    }
}
