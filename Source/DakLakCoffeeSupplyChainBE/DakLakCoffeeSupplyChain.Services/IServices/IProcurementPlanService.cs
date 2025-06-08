using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcurementPlanService
    {
        Task<IServiceResult> GetAll();
    }
}
