using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.WarehouseOutboundRequestEnums
{
    public enum WarehouseOutboundRequestStatus
    {
        Pending,      
        Accepted,       
        Completed,      
        Cancelled,
        PartiallyCompleted,
        Rejected
    }

}
