using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class FarmingCommitmentsDetailRepository
        : GenericRepository<FarmingCommitmentsDetail>, IFarmingCommitmentsDetailRepository
    {
        public FarmingCommitmentsDetailRepository(DakLakCoffee_SCMContext context)
            : base(context) { }

    }
}
