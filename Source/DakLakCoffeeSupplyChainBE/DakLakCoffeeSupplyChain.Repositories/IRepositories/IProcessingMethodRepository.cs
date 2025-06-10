using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProcessingMethodRepository : IGenericRepository<ProcessingMethod>
    {
        Task<ProcessingMethod?> GetDetailByMethodIdAsync(int methodId);
        Task<bool> IsMethodInUseAsync(int methodId);
    }
}
