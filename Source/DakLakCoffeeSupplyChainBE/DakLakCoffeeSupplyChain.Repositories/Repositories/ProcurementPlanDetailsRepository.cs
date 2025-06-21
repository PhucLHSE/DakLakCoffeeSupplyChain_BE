
using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcurementPlanDetailsRepository : GenericRepository<ProcurementPlansDetail>, IProcurementPlanDetailsRepository
    {
        public ProcurementPlanDetailsRepository(DakLakCoffee_SCMContext context) => _context = context;

        public async Task<int> CountProcurementPlanDetailsInYearAsync(int year)
        {
            return await _context.ProcurementPlansDetails
                .CountAsync(p => p.Plan.StartDate.HasValue && p.Plan.StartDate.Value.Year == year);
        }

    }
}
