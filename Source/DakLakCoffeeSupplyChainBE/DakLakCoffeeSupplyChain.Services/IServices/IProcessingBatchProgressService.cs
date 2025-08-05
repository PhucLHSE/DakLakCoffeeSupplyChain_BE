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
        Task<IServiceResult> AdvanceProgressByBatchIdAsync( Guid batchId, AdvanceProcessingBatchProgressDto input, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetByIdAsync(Guid progressId);
        Task<IServiceResult> CreateAsync(Guid batchId,  ProcessingBatchProgressCreateDto input, Guid userId, bool isAdmin, bool isManager);
        Task UpdateMediaUrlsAsync(Guid progressId, string? photoUrl, string? videoUrl);

        Task<IServiceResult> UpdateAsync(Guid progressId, ProcessingBatchProgressUpdateDto input);
        Task<IServiceResult> SoftDeleteAsync(Guid progressId);  
        Task<IServiceResult> HardDeleteAsync(Guid progressId);
    }
}
