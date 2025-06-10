

using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropStageService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(int id);

    }
}
