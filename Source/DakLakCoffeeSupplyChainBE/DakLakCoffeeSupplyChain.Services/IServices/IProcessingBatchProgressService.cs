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
        Task<IServiceResult> GetAllAsync();
        Task<IServiceResult> GetByIdAsync(Guid progressId);
    }
}
