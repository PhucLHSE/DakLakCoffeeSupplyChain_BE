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
        Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetByIdAsync(Guid progressId);
        Task<IServiceResult> CreateAsync(ProcessingBatchProgressCreateDto dto);
        Task<IServiceResult> UpdateAsync(Guid progressId, ProcessingBatchProgressUpdateDto input);
        Task<IServiceResult> SoftDeleteAsync(Guid progressId);
        Task<IServiceResult> HardDeleteAsync(Guid progressId);
    }
}
