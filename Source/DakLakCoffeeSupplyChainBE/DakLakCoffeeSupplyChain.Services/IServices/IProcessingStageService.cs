using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{

    public interface IProcessingStageService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetDetailByIdAsync(int stageId);
        Task<IServiceResult> CreateAsync(CreateProcessingStageDto dto);
        Task<IServiceResult> DeleteAsync(int stageId);
        Task<IServiceResult> UpdateAsync(ProcessingStageUpdateDto dto);
    }
}
