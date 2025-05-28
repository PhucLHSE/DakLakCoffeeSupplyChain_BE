using CoffeeManagement.Flow4.Repositories.Base;
using CoffeeManagement.Flow4.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeManagement.Flow4.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository() { }

        public async Task<User> GetUserAccount(string userName, string password)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == userName
                && u.PasswordHash == password);
        }
    }
}