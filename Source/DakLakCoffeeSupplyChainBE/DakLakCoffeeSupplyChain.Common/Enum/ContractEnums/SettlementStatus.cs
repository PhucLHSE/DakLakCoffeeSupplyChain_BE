using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ContractEnums
{
    public enum SettlementStatus
    {
        None = 0,            // Mặc định khi chưa xử lý đối soát
        Pending = 1,         // Đang chờ chứng từ/đối soát
        PartiallySettled = 2,// Đã chốt một phần (còn khoản treo)
        Settled = 3,         // Đã chốt/khóa sổ toàn bộ
        Disputed = 4,        // Có tranh chấp cần xử lý
        Rejected = 5,        // Từ chối chứng từ/đối soát
        Cancelled = 6,       // Hủy quy trình đối soát
        Overdue = 7          // Quá hạn đối soát theo policy
    }
}
