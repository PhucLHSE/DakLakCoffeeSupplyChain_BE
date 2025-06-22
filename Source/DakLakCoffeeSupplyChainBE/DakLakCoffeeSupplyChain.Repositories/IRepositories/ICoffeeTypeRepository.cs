using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICoffeeTypeRepository : IGenericRepository<CoffeeType>
    {
        Task<int> CountCoffeeTypeInYearAsync(int year);
    }
}
