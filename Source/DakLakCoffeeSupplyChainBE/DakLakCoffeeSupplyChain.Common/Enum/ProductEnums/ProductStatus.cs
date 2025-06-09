using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ProductEnums
{
    public enum ProductStatus
    {
        Draft = 0,            // Mới tạo, chưa gửi duyệt
        Pending = 1,          // Đang chờ phê duyệt từ Business Manager
        Approved = 2,         // Đã được duyệt
        Rejected = 3,         // Bị từ chối
        InStock = 4,          // Đang trong kho
        OutOfStock = 5,       // Hết hàng
        Archived = 6          // Đã ngừng kinh doanh / không còn bán
    }
}
