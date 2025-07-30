using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ShipmentEnums
{
    public enum ShipmentDeliveryStatus
    {
        Pending = 0,       // Mới tạo, chưa giao
        InTransit = 1,     // Đang giao
        Delivered = 2,     // Đã giao thành công
        Failed = 3,        // Giao thất bại
        Returned = 4,      // Đã hoàn trả
        Canceled = 5       // Bị huỷ
    }
}
