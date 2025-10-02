using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcurementPlanRepository : GenericRepository<ProcurementPlan>, IProcurementPlanRepository
    {
        public ProcurementPlanRepository(DakLakCoffee_SCMContext context) 
            => _context = context;

        public async Task<int> CountProcurementPlansInYearAsync(int year)
        {
            return await _context.ProcurementPlans
                .CountAsync(p => 
                   p.StartDate.HasValue && 
                   p.StartDate.Value.Year == year
                );
        }
        public async Task<ProcurementPlan?> GetByIdWithDetailsAsync(Guid planId)
        {
            return await _context.ProcurementPlans
                .Include(p => p.ProcurementPlansDetails)
                .FirstOrDefaultAsync(p => p.PlanId == planId && !p.IsDeleted);
        }
    }
}
