using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICoffeeTypeService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid TypeId);
        Task<IServiceResult> SoftDeleteById(Guid TypeId);
        Task<IServiceResult> DeleteById(Guid TypeId);
    }
}
