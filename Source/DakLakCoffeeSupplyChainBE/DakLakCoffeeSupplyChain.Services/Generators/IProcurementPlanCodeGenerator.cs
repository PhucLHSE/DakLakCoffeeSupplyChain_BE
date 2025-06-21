using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public interface IProcurementPlanCodeGenerator
    {
        Task<string> GenerateProcurementPlanCodeAsync();
        Task<string> GenerateProcurementPlanDetailsCodeAsync();
    }
}
