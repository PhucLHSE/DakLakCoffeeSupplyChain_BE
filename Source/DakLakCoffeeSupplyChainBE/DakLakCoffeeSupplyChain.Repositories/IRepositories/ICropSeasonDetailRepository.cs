using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICropSeasonDetailRepository : IGenericRepository<CropSeasonDetail>
    {
        Task<List<CropSeasonDetail>> GetByCropSeasonIdAsync(Guid cropSeasonId);
        Task<CropSeasonDetail?> GetByIdAsync(Guid detailId);
        Task<bool> ExistsAsync(Expression<Func<CropSeasonDetail, bool>> predicate);
    }
}
