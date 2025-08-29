using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class SystemConfigurationService (IUnitOfWork unitOfWork) : ISystemConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public async Task<IServiceResult> GetAll(Guid userId)
        {
            var admin = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                predicate: u => u.UserId == userId,
                include: u => u.Include( u => u.Role),
                asNoTracking: true
                );
            if (admin == null || !admin.Role.RoleName.Equals("Admin"))
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Tài khoản không có quyền hạn này."
                );
            var configs = await _unitOfWork.SystemConfigurationRepository.GetAllAsync(
                predicate: c => c.IsDeleted != true,
                asNoTracking: true);

            if (configs == null || configs.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<SystemConfigurationViewAllDto>()
                );
            }
            else
            {
                var systemConfigurationViewAllDto = configs
                    .Select(c => c.MapToSystemConfigurationViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    systemConfigurationViewAllDto
                );
            }
        }

        public async Task<IServiceResult> GetByName(string name)
        {
            var configs = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                predicate: c => c.IsDeleted != true && c.Name == name,
                asNoTracking: true);

            if (configs == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<SystemConfigurationViewDetailDto>()
                );
            }
            else
            {
                var response = configs.MapToSystemConfigurationViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    response
                );
            }
        }
    }
}
