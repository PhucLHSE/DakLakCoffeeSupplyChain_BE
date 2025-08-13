using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IGeneralFarmerReportRepository : IGenericRepository<GeneralFarmerReport>
    {
        Task<List<GeneralFarmerReport>> GetAllWithIncludesAsync();

        Task<GeneralFarmerReport?> GetByIdWithIncludesAsync(Guid reportId);

        Task<int> CountReportsInYearAsync(int year);

        Task<GeneralFarmerReport?> GetByIdEvenIfDeletedAsync(Guid reportId);

        Task<string?> GetMaxReportCodeForYearAsync(int year);

        Task<GeneralFarmerReport?> GetByIdAsync(
            Func<GeneralFarmerReport, bool>? predicate = null,
            Func<IQueryable<GeneralFarmerReport>, IQueryable<GeneralFarmerReport>>? include = null,
            bool asNoTracking = false);
        void Update(GeneralFarmerReport entity);
    }
}
