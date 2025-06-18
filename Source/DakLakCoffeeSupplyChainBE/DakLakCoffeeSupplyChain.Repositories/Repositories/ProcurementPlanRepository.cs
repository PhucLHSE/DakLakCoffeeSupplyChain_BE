using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcurementPlanRepository : GenericRepository<ProcurementPlan>, IProcurementPlanRepository
    {
        public ProcurementPlanRepository(DakLakCoffee_SCMContext context) => _context = context;
        public async Task<ProcurementPlan> CreateProcurementPlanAsync(ProcurementPlan procurementPlan)
        {
            _context.ProcurementPlans.Add(procurementPlan);
            await _context.SaveChangesAsync();
            return procurementPlan;
        }
    }
}
