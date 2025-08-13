using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class SystemConfigurationRepository : GenericRepository<SystemConfiguration>, ISystemConfigurationRepository
    {
        public SystemConfigurationRepository() { }

        public SystemConfigurationRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        public async Task<SystemConfiguration?> GetActiveByNameAsync(string name)
        {
            return await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Name == name
                                       && c.IsActive
                                       && c.EffectedDateFrom <= DateTime.Now
                                       && (c.EffectedDateTo == null || c.EffectedDateTo >= DateTime.Now)
                );
        }
    }
}
