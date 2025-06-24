using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.IRepositories.DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcessingBatchProgressRepository : GenericRepository<ProcessingBatchProgress>, IProcessingBatchProgressRepository
    {
        public ProcessingBatchProgressRepository(DakLakCoffee_SCMContext context)
            : base(context)
        {
        }
    }
}
