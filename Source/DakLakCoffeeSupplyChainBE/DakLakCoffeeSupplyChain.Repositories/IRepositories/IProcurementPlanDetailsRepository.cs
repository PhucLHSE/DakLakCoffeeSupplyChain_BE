using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcurementPlanDetailsRepository : IGenericRepository<ProcurementPlansDetail>
    {
        Task<int> CountProcurementPlanDetailsInYearAsync(int year);
    }
}
