
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

    }
}
