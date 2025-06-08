using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICropSeasonRepository : IGenericRepository<CropSeason>
    {
        Task<List<CropSeason>> GetAllCropSeasonsAsync();
        Task<CropSeason?> GetCropSeasonByIdAsync(Guid cropSeasonId);
    }
}
