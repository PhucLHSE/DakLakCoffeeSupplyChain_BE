using DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

public interface IExpertAdviceService
{
    Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin = false);
    Task<IServiceResult> GetAllForManagerAsync();
    Task<IServiceResult> GetByIdAsync(Guid adviceId, Guid userId, bool isAdmin = false);
    Task<IServiceResult> GetExpertAdvicesByReportIdForFarmerAsync(Guid reportId, Guid userId);
    Task<IServiceResult> CreateAsync(ExpertAdviceCreateDto dto, Guid userId);
    Task<IServiceResult> UpdateAsync(Guid adviceId, ExpertAdviceUpdateDto dto, Guid userId);
    Task<IServiceResult> SoftDeleteAsync(Guid adviceId, Guid userId, bool isAdmin = false);
    Task<IServiceResult> HardDeleteAsync(Guid adviceId, Guid userId, bool isAdmin = false);
}
