using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IAgriculturalExpertRepository : IGenericRepository<AgriculturalExpert>
    {
        Task<AgriculturalExpert?> GetByUserIdAsync(Guid userId);
        
        // ✅ Thêm method GetAllAsync
        Task<List<AgriculturalExpert>> GetAllAsync(
            Func<AgriculturalExpert, bool>? predicate = null,
            Func<IQueryable<AgriculturalExpert>, IQueryable<AgriculturalExpert>>? include = null,
            Func<IQueryable<AgriculturalExpert>, IOrderedQueryable<AgriculturalExpert>>? orderBy = null,
            bool asNoTracking = false);
    }
}
