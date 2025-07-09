using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcessingBatchWasteRepository : IGenericRepository<ProcessingBatchWaste>
    {
        Task<List<ProcessingBatchWaste>> GetAllWastesAsync();
        Task<ProcessingBatchWaste?> GetWasteByIdAsync(Guid wasteId);
        Task<int> CountByProgressIdAsync(Guid progressId);
    }
}
