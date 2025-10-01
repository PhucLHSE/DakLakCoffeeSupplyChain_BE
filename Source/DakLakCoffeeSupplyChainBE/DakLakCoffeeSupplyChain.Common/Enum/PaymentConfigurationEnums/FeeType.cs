using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Enum.PaymentConfigurationEnums
{
    public enum FeeType
    {
        Registration = 1,         // Phí đăng ký
        MonthlyMaintenance = 2,   // Phí duy trì tháng
        QuarterlyMaintenance = 3, // Phí duy trì quý
        YearlyMaintenance = 4,    // Phí duy trì năm
        PlanPosting = 5,          // Phí đăng kế hoạch thu mua
        Other = 99                // Phí khác
    }
}
