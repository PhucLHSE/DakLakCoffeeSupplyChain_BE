using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ExpertAdviceRepository : GenericRepository<ExpertAdvice>, IExpertAdviceRepository
    {
        public ExpertAdviceRepository() { }

        public ExpertAdviceRepository(DakLakCoffee_SCMContext context)
            => _context = context;
    }
}
