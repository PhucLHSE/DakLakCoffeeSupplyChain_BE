using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IFarmingCommitmentRepository : IGenericRepository<FarmingCommitment>
    {
        Task<FarmingCommitment?> GetByIdAsync(Guid id);
    }
}
