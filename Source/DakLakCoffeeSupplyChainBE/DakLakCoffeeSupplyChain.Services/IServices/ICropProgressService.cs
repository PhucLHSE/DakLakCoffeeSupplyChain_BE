using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropProgressService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid id);
    }
}
