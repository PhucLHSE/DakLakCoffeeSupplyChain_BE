using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ISystemConfigurationService
    {
        Task<IServiceResult> GetAll(Guid userId);
        Task<IServiceResult> GetByName(string name);
    }
}
