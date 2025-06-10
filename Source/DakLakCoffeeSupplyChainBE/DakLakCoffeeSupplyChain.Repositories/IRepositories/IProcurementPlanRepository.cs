using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcurementPlanRepository : IGenericRepository<ProcurementPlan>
    {
        Task<List<ProcurementPlan>> GetAllProcurementPlansAvailableAsync();
        Task<ProcurementPlan?> GetProcurementPlanByIdAsync(Guid procurementPlanId);
    }
}
