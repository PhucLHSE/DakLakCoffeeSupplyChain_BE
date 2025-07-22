using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ProcurementPlanEnums
{
    //public: màn hình cả tất cả mọi người đều thấy
    //private: màn hình chỉ farmer hoặc BM thấy
    public enum ProcurementPlanDetailsStatus
    {
        Active = 0,   // Trạng thái có hiệu lực, hiển thị ở màn hình public và private, cho phép farmer tiếp tục đăng ký, BM được xóa hoặc khóa bài
        Closed = 1,   // Trạng thái đóng, không thể tiếp tục nộp đơn đăng ký do đã đủ sản lượng, vẫn được hiển thị ở public và private, ở trạng thái này BM không được phép xóa bài
        Disable = 2,  // Trạng thái khóa, không còn hiệu lực và chỉ có màn hình BM thấy, chỉ có thể update sang trạng thái này trong giai đoạn đang thu mua (post vẫn đang mở)
        Unknown = 3,  // Trạng thái không xác định, mặc định khi hệ thống bị lỗi
    }
}
