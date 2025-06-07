using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common.Helpers.Security;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
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
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICodeGenerator _codeGenerator;

        public UserAccountService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
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

        public async Task<IServiceResult> Create(UserAccountCreateDto userDto)
        {
            try
            {
                // Kiểm tra Role (dữ liệu gốc cần có)
                var role = await _unitOfWork.RoleRepository.GetRoleByNameAsync(userDto.RoleName);

                if (role == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Vai trò không hợp lệ.");

                // Kiểm tra email đã tồn tại chưa
                var emailExists = await _unitOfWork.UserAccountRepository.GetUserAccountByEmailAsync(userDto.Email);

                if (emailExists != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Email đã được đăng ký."
                    );
                }

                // Kiểm tra phone đã tồn tại chưa (nếu có nhập)
                if (!string.IsNullOrWhiteSpace(userDto.PhoneNumber))
                {
                    var phoneExists = await _unitOfWork.UserAccountRepository.GetUserAccountByPhoneAsync(userDto.PhoneNumber);

                    if (phoneExists != null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Số điện thoại đã được đăng ký."
                        );
                    }
                }

                // Kiểm tra ngày sinh có hợp lệ không
                if (userDto.DateOfBirth == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Vui lòng nhập ngày sinh."
                    );
                }

                // Lấy cấu hình tuổi tối thiểu
                var config = await _unitOfWork.SystemConfigurationRepository.GetActiveByNameAsync("MIN_AGE_FOR_REGISTRATION");

                // Mặc định 18 nếu chưa có cấu hình
                int minAge = (int)(config?.MinValue ?? 18);

                int actualAge = DateHelper.CalculateAge(userDto.DateOfBirth.Value);

                if (actualAge < minAge)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Người dùng phải từ {minAge} tuổi trở lên."
                    );
                }

                // Generate password hash và user code
                string passwordHash = _passwordHasher.Hash(userDto.Password); // hoặc bất kỳ method nào của bạn
                string userCode = await _codeGenerator.GenerateUserCodeAsync(); // ví dụ: "USR-YYYY-####" hoặc Guid, tuỳ bạn

                // Map DTO to Entity
                var newUser = userDto.MapToUserAccountCreateDto(passwordHash, userCode, role.RoleId);

                // Save data to database
                await _unitOfWork.UserAccountRepository.CreateAsync(newUser);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = newUser.MapToUserAccountViewDetailsDto();
                    responseDto.RoleName = role.RoleName;

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
