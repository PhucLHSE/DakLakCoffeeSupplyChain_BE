using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IFarmingCommitmentService
    {
        Task<IServiceResult> GetAllBusinessManagerCommitment(Guid userId);
        Task<IServiceResult> GetAllFarmerCommitment(Guid userId);
        Task<IServiceResult> GetById(Guid commitmentId);
        Task<IServiceResult> GetAvailableForCropSeason(Guid userId);

    }
}
