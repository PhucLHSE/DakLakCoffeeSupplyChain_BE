using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingBatchProgressService
    {
        Task<IServiceResult> AdvanceProgressByBatchIdAsync(Guid batchId, AdvanceProcessingBatchProgressDto input, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> UpdateProgressAfterEvaluationAsync(Guid batchId, ProcessingBatchProgressCreateDto input, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetAllByBatchIdAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetByIdAsync(Guid id);
        Task<IServiceResult> CreateAsync(Guid batchId, ProcessingBatchProgressCreateDto input, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> CreateWithMediaAndWasteAsync(Guid batchId, ProcessingBatchProgressCreateRequest input, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> AdvanceWithMediaAndWasteAsync(Guid batchId, AdvanceProcessingBatchProgressRequest input, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> UpdateAsync(Guid id, ProcessingBatchProgressUpdateDto input);
        Task<IServiceResult> SoftDeleteAsync(Guid id);
        Task<IServiceResult> HardDeleteAsync(Guid progressId);
        Task<IServiceResult> AdvanceProgressAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetAvailableBatchesForProgressAsync(Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> UpdateNextStagesAsync(Guid batchId, ProcessingBatchProgressCreateDto input, Guid userId, bool isAdmin, bool isManager);
    }
}
