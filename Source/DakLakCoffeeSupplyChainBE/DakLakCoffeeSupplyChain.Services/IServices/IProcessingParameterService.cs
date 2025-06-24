using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingParameterService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid id);
        Task<IServiceResult> CreateAsync(ProcessingParameterCreateDto dto);
        Task<IServiceResult> UpdateAsync(ProcessingParameterUpdateDto dto);
        Task<IServiceResult> SoftDeleteAsync(Guid parameterId);
        Task<IServiceResult> HardDeleteAsync(Guid parameterId);
    }
}
