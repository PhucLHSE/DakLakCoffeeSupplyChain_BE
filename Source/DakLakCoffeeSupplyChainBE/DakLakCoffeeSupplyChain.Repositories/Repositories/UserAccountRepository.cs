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
        public UserAccountRepository() { }
        
        public UserAccountRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        public async Task<List<UserAccount>> GetAllUserAccountsAsync()
        {
            var userAccounts = await _context.UserAccounts
                .AsNoTracking()
                .Include(m => m.Role)
                .OrderBy(u => u.UserCode)
                .ToListAsync();

            return userAccounts;
        }

        public async Task<UserAccount?> GetUserAccountByIdAsync(Guid userId)
        {
            var user = await _context.UserAccounts
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            return user;
        }

        public async Task<UserAccount?> GetUserAccountByEmailAsync(string email)
        {
            return await _context.UserAccounts
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserAccount?> GetUserAccountByPhoneAsync(string phoneNumber)
        {
            return await _context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<int> CountUsersRegisteredInYearAsync(int year)
        {
            return await _context.UserAccounts
                .CountAsync(u => u.RegistrationDate.Year == year);
        }
       
    }
}
