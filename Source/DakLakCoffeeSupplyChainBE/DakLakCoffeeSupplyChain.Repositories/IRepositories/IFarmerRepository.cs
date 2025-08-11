using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IFarmerRepository : IGenericRepository<Farmer>
    {
        Task<Farmer?> GetByIdAsync(Guid id);

        Task<Farmer?> FindByUserIdAsync(Guid userId);

        Task<int> CountFarmerInYearAsync(int year);
    }
}
