using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IGeneralFarmerReportRepository : IGenericRepository<GeneralFarmerReport>
    {
        Task<List<GeneralFarmerReport>> GetAllWithIncludesAsync();
        Task<GeneralFarmerReport?> GetByIdWithIncludesAsync(Guid reportId);
    }
}
