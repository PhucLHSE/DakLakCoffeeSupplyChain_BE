using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums   
{
    public enum ProcessingStatus
    {
        [Display(Name = "Chưa bắt đầu")]
        NotStarted = 0,

        [Display(Name = "Đang xử lý")]
        InProgress = 1,

        [Display(Name = "Hoàn thành")]
        Completed = 2,

        [Display(Name = "Đã hủy")]
        Cancelled = 3
    }
}
