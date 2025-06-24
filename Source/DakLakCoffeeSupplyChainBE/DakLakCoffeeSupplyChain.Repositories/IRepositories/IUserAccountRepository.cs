using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IUserAccountRepository : IGenericRepository<UserAccount>
    {
        Task<List<UserAccount>> GetAllUserAccountsAsync();

        Task<UserAccount?> GetUserAccountByIdAsync(Guid userId);

        Task<UserAccount?> GetUserAccountByEmailAsync(string email);

        Task<UserAccount?> GetUserAccountByPhoneAsync(string phoneNumber);

        Task<int> CountUsersRegisteredInYearAsync(int year);
       
    }
}
