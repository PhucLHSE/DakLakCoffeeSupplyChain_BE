using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ContractDeliveryBatchEnums
{
    public enum ContractDeliveryBatchStatus
    {
        Planned = 0,       // Đã lên kế hoạch
        InProgress = 1,    // Đang giao hàng
        Fulfilled = 2,     // Đã hoàn thành
        Cancelled = 3,     // Huỷ
    }
}
