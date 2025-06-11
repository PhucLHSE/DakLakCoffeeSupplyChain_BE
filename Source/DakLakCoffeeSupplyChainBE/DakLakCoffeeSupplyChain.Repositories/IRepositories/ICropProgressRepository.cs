using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICropProgressRepository : IGenericRepository<CropProgress>
    {
        Task<List<CropProgress>> GetAllWithIncludesAsync();
        Task<CropProgress?> GetByIdWithIncludesAsync(Guid progressId);
    }
}
