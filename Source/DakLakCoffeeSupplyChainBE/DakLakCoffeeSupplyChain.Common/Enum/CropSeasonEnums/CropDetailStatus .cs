using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums
{
    public enum CropDetailStatus
    {
        [Display(Name = "Đã lên kế hoạch")]
        Planned = 0,

        [Display(Name = "Đang canh tác")]
        InProgress = 1,

        [Display(Name = "Đã hoàn thành")]
        Completed = 2,

        [Display(Name = "Đã hủy")]
        Cancelled =3 
    }
}
