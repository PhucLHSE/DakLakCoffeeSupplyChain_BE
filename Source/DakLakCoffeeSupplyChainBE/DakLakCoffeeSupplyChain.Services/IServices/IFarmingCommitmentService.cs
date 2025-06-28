using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IFarmingCommitmentService
    {
        Task<IServiceResult> GetAll(Guid userId);
        Task<IServiceResult> GetById(Guid commitmentId);
    }
}
