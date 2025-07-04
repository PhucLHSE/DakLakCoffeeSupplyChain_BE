using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.OrderEnums
{
    public enum OrderStatus
    {
        Pending = 0,            // Đơn hàng mới tạo, chưa xử lý
        Preparing = 1,          // Đang chuẩn bị giao (xuất kho, đóng gói)
        Shipped = 2,            // Đã xuất hàng
        Delivered = 3,          // Giao hàng hoàn tất
        Cancelled = 4,          // Bị huỷ do lý do nội bộ hoặc khách
        Failed = 5              // Giao hàng thất bại
    }
}
