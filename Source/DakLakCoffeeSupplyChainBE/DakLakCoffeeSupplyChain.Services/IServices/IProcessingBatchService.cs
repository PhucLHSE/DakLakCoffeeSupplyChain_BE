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
        Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin, bool isManager, bool isExpert = false);
        Task<IServiceResult> CreateAsync(ProcessingBatchCreateDto dto, Guid userId);
        Task<IServiceResult> UpdateAsync(ProcessingBatchUpdateDto dto, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> SoftDeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> HardDeleteAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager);
        Task<IServiceResult> GetAvailableProcessingDataAsync(Guid userId, Guid? cropSeasonId = null);
        Task<IServiceResult> GetFullDetailsAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert = false);
        Task<IServiceResult> GetBatchesByFarmerForBusinessManagerAsync(Guid managerUserId, Guid farmerId);
        Task<IServiceResult> GetFarmersWithBatchesForBusinessManagerAsync(Guid managerUserId);
    }
}
