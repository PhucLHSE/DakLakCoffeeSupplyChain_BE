using CoffeeManagement.Flow4.Repositories;
using CoffeeManagement.Flow4.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeManagement.Flow4.Services
{
    public class UserAccountsService
    {
        private readonly UserRepository _userRepository;

        public UserAccountsService()
        {
            _userRepository = new UserRepository();
        }

        public async Task<User> Authenticate(string Email, string password)
        {
            return await _userRepository.GetUserAccount(Email, password);
        }
    }
}