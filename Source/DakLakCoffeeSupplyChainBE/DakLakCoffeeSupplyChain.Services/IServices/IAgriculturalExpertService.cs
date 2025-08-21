using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IAgriculturalExpertService
    {
        Task<IServiceResult> GetAllAsync();
        Task<IServiceResult> GetByIdAsync(Guid expertId);
        Task<IServiceResult> GetByUserIdAsync(Guid userId);
        Task<IServiceResult> CreateAsync(AgriculturalExpertCreateDto dto, Guid userId);
        Task<IServiceResult> UpdateAsync(AgriculturalExpertUpdateDto dto, Guid userId, string userRole);
        Task<IServiceResult> DeleteAsync(Guid expertId);
        Task<IServiceResult> SoftDeleteAsync(Guid expertId, Guid userId, string userRole);
        Task<IServiceResult> GetVerifiedExpertsAsync();
    }
}
