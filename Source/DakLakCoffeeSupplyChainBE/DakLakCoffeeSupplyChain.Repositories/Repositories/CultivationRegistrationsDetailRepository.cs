using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CultivationRegistrationsDetailRepository : GenericRepository<CultivationRegistrationsDetail>, ICultivationRegistrationsDetailRepository
    {
        public CultivationRegistrationsDetailRepository(DakLakCoffee_SCMContext context) => _context = context;
    }
}
