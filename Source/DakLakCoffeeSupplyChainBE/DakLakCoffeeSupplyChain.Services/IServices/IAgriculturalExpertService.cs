using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IAgriculturalExpertService
    {
        Task<IServiceResult> GetAllAsync();
        Task<IServiceResult> GetByIdAsync(Guid expertId);
    }
}
