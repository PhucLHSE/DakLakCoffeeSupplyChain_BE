using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums
{
    public enum CropSeasonStatus
    {
        [Display(Name = "Đang hoạt động")]
        Active = 0,

        [Display(Name = "Tạm dừng")]
        Paused = 1,

        [Display(Name = "Hoàn thành")]
        Completed = 2,

        [Display(Name = "Đã hủy")]
        Cancelled = 3
    }
}
