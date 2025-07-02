using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.Base;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IAgriculturalExpertRepository : IGenericRepository<AgriculturalExpert>
    {
        Task<int> CountVerifiedExpertsAsync();
    }
}
