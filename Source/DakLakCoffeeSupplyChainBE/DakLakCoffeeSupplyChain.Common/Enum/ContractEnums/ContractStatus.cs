using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ContractEnums
{
    public enum ContractStatus
    {
        NotStarted = 0,         // Đã nhập hợp đồng, chưa tạo đợt giao nào
        PreparingDelivery = 1,  // Đã tạo đợt giao, chưa có lô nào giao về
        InProgress = 2,         // Đang thực hiện – đã có hàng bắt đầu giao
        PartialCompleted = 3,   // Giao được một phần, hết thời hạn 1 số đợt
        Completed = 4,          // Giao đủ toàn bộ theo hợp đồng
        Cancelled = 5,          // Hợp đồng bị hủy giữa chừng
        Expired = 6             // Quá thời hạn kết thúc mà chưa hoàn tất
    }
}
