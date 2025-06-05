using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IUserAccountRepository
    {
        Task<List<UserAccount>> GetAllUserAccountsAsync();

        Task<UserAccount?> GetUserAccountByIdAsync(Guid userId);
    }
}
