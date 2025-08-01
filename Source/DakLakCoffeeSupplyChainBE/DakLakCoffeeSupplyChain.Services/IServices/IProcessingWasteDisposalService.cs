using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingWasteDisposalService
    {
        Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin);

        Task<IServiceResult> GetByIdAsync(Guid disposalId);


        Task<IServiceResult> UpdateAsync(Guid id, ProcessingWasteDisposalUpdateDto dto, Guid userId);


        Task<IServiceResult> CreateAsync(ProcessingWasteDisposalCreateDto dto, Guid userId);

        Task<IServiceResult> SoftDeleteAsync(Guid disposalId, Guid userId, bool isManager);

        Task<IServiceResult> HardDeleteAsync(Guid disposalId, Guid userId, bool isManager);
        Task<bool> HasPermissionToDeleteAsync(Guid disposalId, Guid userId);
    }
}
