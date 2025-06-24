using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICultivationRegistrationService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid registrationId);
    }
}
