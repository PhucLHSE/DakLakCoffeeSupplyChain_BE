using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserAccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {

            var userAccounts = await _unitOfWork.UserAccountRepository.GetAllUserAccountsAsync();

            if (userAccounts == null || !userAccounts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<UserAccountViewAllDto>()
                );
            }
            else
            {
                var userAccountDtos = userAccounts
                    .Select(userAccounts => userAccounts.MapToUserAccountViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    userAccountDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid userId)
        {
            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new UserAccountViewDetailsDto()
                );
            }
            else
            {
                var userDto = user.MapToUserAccountViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    userDto
                );
            }
        }
    }
}
