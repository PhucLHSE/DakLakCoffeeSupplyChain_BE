using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcurementPlanDetailsRepository : IGenericRepository<ProcurementPlansDetail>
    {
        Task<List<ProcurementPlansDetail>> GetAllProcurementPlanDetailsInSamePlanAsync(Guid planId);
    }
}
