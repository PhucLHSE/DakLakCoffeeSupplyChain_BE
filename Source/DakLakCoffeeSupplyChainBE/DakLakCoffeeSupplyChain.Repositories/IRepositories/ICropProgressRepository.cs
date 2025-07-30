using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICropProgressRepository : IGenericRepository<CropProgress>
    {
        Task<List<CropProgress>> FindAsync(Expression<Func<CropProgress, bool>> predicate);
        Task<List<CropProgress>> GetAllWithIncludesAsync();
        Task<List<CropProgress>> GetByCropSeasonDetailIdWithIncludesAsync(Guid cropSeasonDetailId, Guid userId);
        Task<CropProgress?> GetByIdWithDetailAsync(Guid progressId);
        Task<CropProgress?> GetByIdWithIncludesAsync(Guid progressId);

    }
}
