using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcessingStageRepository : IGenericRepository<ProcessingStage>
    {
        Task<List<ProcessingStage>> GetAllStagesAsync();  
        Task CreateAsync(ProcessingStage entity);
        Task<bool> SoftDeleteAsync(int stageId);
    }
}
