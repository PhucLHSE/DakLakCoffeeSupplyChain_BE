using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcessingParameterRepository : IGenericRepository<ProcessingParameter>
    {
        Task<List<ProcessingParameter>> GetAllActiveAsync();
        Task<ProcessingParameter?> GetByIdAsync(Guid parameterId);

    }
}
