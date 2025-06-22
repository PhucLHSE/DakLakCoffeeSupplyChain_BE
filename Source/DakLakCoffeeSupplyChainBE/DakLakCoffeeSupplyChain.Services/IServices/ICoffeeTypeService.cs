using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICoffeeTypeService
    {
        Task<IServiceResult> GetAll();
    }
}
