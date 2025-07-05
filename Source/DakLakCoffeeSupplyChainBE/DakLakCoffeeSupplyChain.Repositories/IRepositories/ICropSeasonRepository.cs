using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICropSeasonRepository : IGenericRepository<CropSeason>
    {
        Task<List<CropSeason>> GetAllCropSeasonsAsync();
        Task<CropSeason?> GetCropSeasonByIdAsync(Guid cropSeasonId);
        Task<int> CountByYearAsync(int year);

        Task<CropSeason?> GetWithDetailsByIdAsync(Guid cropSeasonId);

        Task DeleteCropSeasonDetailsBySeasonIdAsync(Guid cropSeasonId);

        Task<bool> ExistsAsync(Expression<Func<CropSeason, bool>> predicate);
        IQueryable<CropSeason> GetQuery();


    }
}
