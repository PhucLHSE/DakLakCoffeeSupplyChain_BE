using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IBusinessManagerRepository : IGenericRepository<BusinessManager>
    {
        Task<BusinessManager?> GetByUserIdAsync(Guid userId);

        Task<BusinessManager?> GetByTaxIdAsync(string taxId);

        Task<int> CountBusinessManagersRegisteredInYearAsync(int year);
        Task<BusinessManager?> FindByUserIdAsync(Guid userId);
    }
}
