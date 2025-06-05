using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Generators
{
    public class UserCodeGenerator : ICodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserCodeGenerator(IUnitOfWork unitOfWork)
            => _unitOfWork = unitOfWork;

        public async Task<string> GenerateUserCodeAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            // Đếm số user tạo trong năm
            var count = await _unitOfWork.UserAccountRepository.CountUsersRegisteredInYearAsync(currentYear);

            return $"USR-{currentYear}-{(count + 1):D4}";
        }
    }
}
