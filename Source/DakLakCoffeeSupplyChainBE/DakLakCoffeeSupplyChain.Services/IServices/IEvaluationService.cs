using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IEvaluationService
    {
        Task<IServiceResult> CreateAsync(EvaluationCreateDto dto, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> UpdateAsync(Guid id, EvaluationUpdateDto dto, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> DeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> HardDeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> BulkHardDeleteAsync(List<Guid> ids, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> RestoreAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> GetByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> GetSummaryByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<IServiceResult> GetDeletedByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert);
        Task<List<ProcessingStage>> GetFailedStagesForBatchAsync(Guid batchId);
    }
}
