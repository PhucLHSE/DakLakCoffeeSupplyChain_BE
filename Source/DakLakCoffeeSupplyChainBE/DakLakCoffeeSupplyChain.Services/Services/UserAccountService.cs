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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
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
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _passwordHasher = passwordHasher 
                ?? throw new ArgumentNullException(nameof(passwordHasher));

            _codeGenerator = codeGenerator 
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Truy vấn tất cả người dùng từ repository
            var userAccounts = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => u.IsDeleted != true,
                include: query => query.Include(u => u.Role),
                orderBy: u => u.OrderBy(u => u.UserCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (userAccounts == null || !userAccounts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<UserAccountViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
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
            // Tìm tài khoản người dùng theo ID
            var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                predicate: u => u.UserId == userId,
                include: query => query.Include(u => u.Role),
                asNoTracking: true
            );

            // Trả về cảnh báo nếu không tìm thấy
            if (user == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new UserAccountViewDetailsDto()   // Trả về DTO rỗng
                );
            }
            else
            {
                // Map entity sang DTO chi tiết
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
                var newUser = userDto.MapToNewUserAccount(passwordHash, userCode, role.RoleId);

                // Tạo người dùng ở repository
                await _unitOfWork.UserAccountRepository.CreateAsync(newUser);

                // Lưu thay đổi vào database
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

        public async Task<IServiceResult> Update(UserAccountUpdateDto userDto)
        {
            try
            {
                // Kiểm tra User tồn tại
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userDto.UserId);

                if (user == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Người dùng không tồn tại."
                    );
                }

                // Kiểm tra RoleName → RoleId
                var role = await _unitOfWork.RoleRepository.GetRoleByNameAsync(userDto.RoleName);

                if (role == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Vai trò không hợp lệ."
                    );
                }

                // Kiểm tra Email đã tồn tại ở người khác chưa
                var emailUser = await _unitOfWork.UserAccountRepository.GetUserAccountByEmailAsync(userDto.Email);

                if (emailUser != null && emailUser.UserId != user.UserId)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Email đã được sử dụng bởi tài khoản khác."
                    );
                }

                // Kiểm tra SĐT đã tồn tại ở người khác chưa
                if (!string.IsNullOrWhiteSpace(userDto.PhoneNumber))
                {
                    var phoneUser = await _unitOfWork.UserAccountRepository.GetUserAccountByPhoneAsync(userDto.PhoneNumber);

                    if (phoneUser != null && phoneUser.UserId != user.UserId)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE, 
                            "Số điện thoại đã được sử dụng bởi tài khoản khác."
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
                        Const.FAIL_UPDATE_CODE,
                        $"Người dùng phải từ {minAge} tuổi trở lên."
                    );
                }

                //Map DTO to Entity
                userDto.MapToUpdateUserAccount(user, role.RoleId);

                // Cập nhật người dùng ở repository
                await _unitOfWork.UserAccountRepository.UpdateAsync(user);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = user.MapToUserAccountViewDetailsDto();
                    responseDto.RoleName = role.RoleName;

                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
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

        public async Task<IServiceResult> DeleteById(Guid userId)
        {
            try
            {
                // Tìm tài khoản người dùng theo ID từ repository
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    include: query => query.Include(u => u.Role),
                    asNoTracking: true
                );

                // Nếu không tìm thấy người dùng, trả về cảnh báo không có dữ liệu
                if (user == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa người dùng ra khỏi repository
                    await _unitOfWork.UserAccountRepository.RemoveAsync(user);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid userId)
        {
            try
            {
                // Tìm tài khoản người dùng theo ID từ repository
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    include: query => query.Include(u => u.Role),
                    asNoTracking: true
                );

                // Nếu không tìm thấy người dùng, trả về cảnh báo không có dữ liệu
                if (user == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    user.IsDeleted = true;
                    user.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm vai trò ở repository
                    await _unitOfWork.UserAccountRepository.UpdateAsync(user);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
