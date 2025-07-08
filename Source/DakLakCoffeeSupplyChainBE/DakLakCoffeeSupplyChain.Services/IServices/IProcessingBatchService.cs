using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingBatchService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin = false);
        Task<IServiceResult> CreateAsync(ProcessingBatchCreateDto dto, Guid userId);
        Task<IServiceResult> UpdateAsync(ProcessingBatchUpdateDto dto, Guid userId);
        Task<IServiceResult> SoftDeleteAsync(Guid batchId, Guid userId);
        Task<IServiceResult> HardDeleteAsync(Guid batchId, Guid userId);
    }
}
