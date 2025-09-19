using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingMethodService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(int methodId);

        Task<IServiceResult> DeleteById(int methodId);

        Task<IServiceResult> CreateAsync(ProcessingMethodCreateDto input);
        Task<IServiceResult> UpdateAsync(ProcessingMethodUpdateDto input);
        Task<IServiceResult> SoftDeleteAsync(int methodId);

    }
}
