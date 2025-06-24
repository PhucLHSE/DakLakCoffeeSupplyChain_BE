using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICultivationRegistrationService
    {
        Task<IServiceResult> GetAll();
    }
}
