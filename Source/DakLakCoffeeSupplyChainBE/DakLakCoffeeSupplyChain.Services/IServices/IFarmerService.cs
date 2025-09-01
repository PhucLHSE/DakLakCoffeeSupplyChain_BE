using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IFarmerService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid farmerId);
        Task<IServiceResult> SoftDeleteById(Guid farmerId);
        Task<IServiceResult> DeleteById(Guid farmerId);
        Task<IServiceResult> VerifyFarmer(Guid farmerId, bool isVerified);
    }
}
