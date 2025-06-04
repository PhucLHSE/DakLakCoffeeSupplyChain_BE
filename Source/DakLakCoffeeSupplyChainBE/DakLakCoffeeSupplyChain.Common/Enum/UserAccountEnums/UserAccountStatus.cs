using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums
{
    public enum UserAccountStatus
    {
        Unknown = -1,          // Trạng thái chưa xác định, dùng làm default
        PendingApproval = 0,   // Mới tạo, chưa được duyệt
        Active = 1,            // Đã được duyệt và đang hoạt động
        Inactive = 2,          // Đã kích hoạt nhưng đang tạm ngưng
        Locked = 3,            // Bị khóa tạm thời (quản trị viên can thiệp)
        Suspended = 4,         // Tạm đình chỉ (nghi ngờ vi phạm)
        Rejected = 5,          // Đăng ký nhưng bị từ chối duyệt
        Deleted = 6,           // Xóa mềm (không thể đăng nhập)
        Banned = 7             // Đưa vào danh sách đen (vi phạm nặng)
    }
}
