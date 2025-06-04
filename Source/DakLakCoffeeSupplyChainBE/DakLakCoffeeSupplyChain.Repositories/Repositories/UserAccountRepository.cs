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
    public class UserAccountRepository : GenericRepository<UserAccount>, IUserAccountRepository
    {
        public UserAccountRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        public async Task<List<UserAccount>> GetAllUserAccountAsync()
        {
            var userAccounts = await _context.UserAccounts
                .AsNoTracking()
                .Include(m => m.Role)
                .OrderBy(u => u.UserCode)
                .ToListAsync();

            return userAccounts;
        }
    }
}
