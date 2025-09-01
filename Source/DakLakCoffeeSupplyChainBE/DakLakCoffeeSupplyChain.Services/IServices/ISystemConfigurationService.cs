using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ISystemConfigurationService
    {
        Task<IServiceResult> GetAll(Guid userId);
        Task<IServiceResult> GetByName(string name);
        
        // ========== CRUD CHO TIÊU CHÍ ĐÁNH GIÁ CHẤT LƯỢNG ==========
        Task<IServiceResult> GetProcessingBatchCriteriaAsync(); // Lấy tiêu chí cho ProcessingBatch
        Task<IServiceResult> CreateProcessingBatchCriteriaAsync(CreateProcessingBatchCriteriaDto dto, Guid userId);
        Task<IServiceResult> UpdateProcessingBatchCriteriaAsync(string name, UpdateProcessingBatchCriteriaDto dto, Guid userId);
        Task<IServiceResult> DeleteProcessingBatchCriteriaAsync(string name, Guid userId);
        Task<IServiceResult> ActivateProcessingBatchCriteriaAsync(string name, Guid userId);
        Task<IServiceResult> DeactivateProcessingBatchCriteriaAsync(string name, Guid userId);
        Task<IServiceResult> GetProcessingBatchCriteriaByIdAsync(int id);
        Task<IServiceResult> GetProcessingBatchCriteriaByNameAsync(string name);
    }
}
