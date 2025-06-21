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
    public class CodeGenerator : ICodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork;

        public CodeGenerator(IUnitOfWork unitOfWork)
            => _unitOfWork = unitOfWork;

        public async Task<string> GenerateUserCodeAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            // Đếm số user tạo trong năm
            var count = await _unitOfWork.UserAccountRepository.CountUsersRegisteredInYearAsync(currentYear);

            return $"USR-{currentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateManagerCodeAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            // Đếm số manager tạo trong năm
            var count = await _unitOfWork.BusinessManagerRepository.CountBusinessManagersRegisteredInYearAsync(currentYear);

            return $"BM-{currentYear}-{(count + 1):D4}";
        }

        public async Task<string> GenerateBuyerCodeAsync(Guid managerId)
        {
            var currentYear = DateTime.UtcNow.Year;

            // Đếm số buyer mà manager này đã tạo trong năm
            var count = await _unitOfWork.BusinessBuyerRepository.CountBuyersCreatedByManagerInYearAsync(managerId, currentYear);

            // Lấy ManagerCode để dùng làm tiền tố
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => m.ManagerId == managerId && !m.IsDeleted,
                asNoTracking: true
            );

            var managerCode = manager?.ManagerCode;

            if (string.IsNullOrWhiteSpace(managerCode))
                managerCode = "BM-UNKNOWN"; // fallback

            return $"{managerCode}-BUY-{currentYear}-{(count + 1):D3}";
        }
    }
}
