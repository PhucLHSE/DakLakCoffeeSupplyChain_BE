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
using System.Data;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICodeGenerator _codeGenerator;

        public UserAccountService(
            IUnitOfWork unitOfWork, 
            IPasswordHasher passwordHasher, 
            ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _passwordHasher = passwordHasher 
                ?? throw new ArgumentNullException(nameof(passwordHasher));

            _codeGenerator = codeGenerator 
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId, string userRole)
        {
            // Kiểm tra quyền truy cập
            if (userRole != "Admin" && 
                userRole != "BusinessManager")
            {
                return new ServiceResult(
                    Const.FAIL_READ_CODE,
                    "Bạn không có quyền truy cập danh sách người dùng."
                );
            }

            // Danh sách người dùng sẽ được gán tùy theo vai trò (Admin hoặc BusinessManager)
            List<UserAccount> userAccounts;

            // Truy vấn người dùng từ repository
            if (userRole == "Admin")
            {
                // Admin có quyền xem toàn bộ người dùng
                userAccounts = await _unitOfWork.UserAccountRepository.GetAllAsync(
                    predicate: u => !u.IsDeleted,
                    include: query => query
                       .Include(u => u.Role),
                    orderBy: u => u.OrderBy(u => u.UserCode),
                    asNoTracking: true
                );
            }
            else // BusinessManager
            {
                // Chỉ lấy các user là staff dưới quyền BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.FAIL_READ_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

                var staffs = await _unitOfWork.BusinessStaffRepository.GetAllAsync(
                    predicate: s => 
                       s.SupervisorId == manager.ManagerId && 
                       !s.IsDeleted,
                    include: s => s
                       .Include(st => st.User)
                          .ThenInclude(u => u.Role),
                    asNoTracking: true
                );

                userAccounts = staffs
                    .Where(s => s.User != null)
                    .Select(s => s.User!)
                    .ToList();
            }

            // Kiểm tra nếu không có dữ liệu
            if (userAccounts == null || 
                !userAccounts.Any())
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
                    .Select(userAccount => userAccount.MapToUserAccountViewAllDto())
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
                predicate: u => 
                   u.UserId == userId && 
                   !u.IsDeleted,
                include: query => query
                   .Include(u => u.Role),
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

        public async Task<IServiceResult> Create(UserAccountCreateDto userDto, Guid userId, string userRole)
        {
            try
            {
                // Kiểm tra quyền truy cập
                if (userRole != "Admin" && 
                    userRole != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Bạn không có quyền tạo tài khoản người dùng."
                    );
                }

                // Vai trò để tạo – Chỉ BusinessManager bị giới hạn
                if (userRole == "BusinessManager" && 
                    userDto.RoleName != "BusinessStaff")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "BusinessManager chỉ được phép tạo tài khoản nhân viên doanh nghiệp (BusinessStaff)."
                    );
                }

                // Kiểm tra Role (dữ liệu gốc cần có)
                var role = await _unitOfWork.RoleRepository
                    .GetRoleByNameAsync(userDto.RoleName);

                if (role == null)
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Vai trò không hợp lệ."
                    );

                // Kiểm tra email đã tồn tại chưa
                var emailExists = await _unitOfWork.UserAccountRepository
                    .GetUserAccountByEmailAsync(userDto.Email);

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
                    var phoneExists = await _unitOfWork.UserAccountRepository
                        .GetUserAccountByPhoneAsync(userDto.PhoneNumber);

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
                var config = await _unitOfWork.SystemConfigurationRepository
                    .GetActiveByNameAsync("MIN_AGE_FOR_REGISTRATION");

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

                // Tạo password hash và user code
                string passwordHash = _passwordHasher.Hash(userDto.Password); // hoặc bất kỳ method nào của bạn
                string userCode = await _codeGenerator.GenerateUserCodeAsync(); // ví dụ: "USR-YYYY-####" hoặc Guid, tuỳ bạn

                // Ánh xạ dữ liệu từ DTO vào entity
                var newUser = userDto.MapToNewUserAccount(passwordHash, userCode, role.RoleId);

                // Tạo người dùng ở repository
                await _unitOfWork.UserAccountRepository.CreateAsync(newUser);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
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
                var user = await _unitOfWork.UserAccountRepository
                    .GetByIdAsync(userDto.UserId);

                if (user == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Người dùng không tồn tại."
                    );
                }

                // Kiểm tra RoleName → RoleId
                var role = await _unitOfWork.RoleRepository
                    .GetRoleByNameAsync(userDto.RoleName);

                if (role == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Vai trò không hợp lệ."
                    );
                }

                // Kiểm tra Email đã tồn tại ở người khác chưa
                var emailUser = await _unitOfWork.UserAccountRepository
                    .GetUserAccountByEmailAsync(userDto.Email);

                if (emailUser != null && 
                    emailUser.UserId != user.UserId)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Email đã được sử dụng bởi tài khoản khác."
                    );
                }

                // Kiểm tra SĐT đã tồn tại ở người khác chưa
                if (!string.IsNullOrWhiteSpace(userDto.PhoneNumber))
                {
                    var phoneUser = await _unitOfWork.UserAccountRepository
                        .GetUserAccountByPhoneAsync(userDto.PhoneNumber);

                    if (phoneUser != null && 
                        phoneUser.UserId != user.UserId)
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
                var config = await _unitOfWork.SystemConfigurationRepository
                    .GetActiveByNameAsync("MIN_AGE_FOR_REGISTRATION");

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

                // Ánh xạ dữ liệu từ DTO vào entity
                userDto.MapToUpdateUserAccount(user, role.RoleId);

                // Cập nhật người dùng ở repository
                await _unitOfWork.UserAccountRepository.UpdateAsync(user);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
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

        public async Task<IServiceResult> DeleteUserAccountById(Guid userId, Guid currentUserId, string currentUserRole)
        {
            try
            {
                // Admin có toàn quyền xóa
                if (currentUserRole != "Admin" && 
                    currentUserRole != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Bạn không có quyền xóa người dùng."
                    );
                }

                // Tìm tài khoản người dùng theo ID từ repository
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    include: query => query
                       .Include(u => u.Role),
                    asNoTracking: false
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
                    // Nếu là BusinessManager → kiểm tra quyền giám sát
                    if (currentUserRole == "BusinessManager")
                    {
                        var manager = await _unitOfWork.BusinessManagerRepository.GetByUserIdAsync(currentUserId);

                        if (manager == null)
                        {
                            return new ServiceResult(
                                Const.FAIL_DELETE_CODE,
                                "Không tìm thấy thông tin BusinessManager hiện tại."
                            );
                        }

                        var staff = await _unitOfWork.BusinessStaffRepository
                            .GetByUserIdAsync(userId);

                        if (staff == null || 
                            staff.SupervisorId != manager.ManagerId)
                        {
                            return new ServiceResult(
                                Const.FAIL_DELETE_CODE,
                                "Bạn không có quyền xóa người dùng này."
                            );
                        }
                    }

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

        public async Task<IServiceResult> SoftDeleteUserAccountById(Guid userId)
        {
            try
            {
                // Tìm tài khoản người dùng theo ID từ repository
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    asNoTracking: false
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

        public async Task<bool> CanAccessUser(Guid currentUserId, string currentUserRole, Guid targetUserId)
        {
            // Admin xem được tất cả
            if (currentUserRole == "Admin")
                return true;

            // Ai cũng được xem chính mình
            if (currentUserId == targetUserId)
                return true;

            // BusinessManager được xem các BusinessStaff thuộc doanh nghiệp mình
            if (currentUserRole == "BusinessManager")
            {
                var manager = await _unitOfWork.BusinessManagerRepository
                    .GetByUserIdAsync(currentUserId);

                if (manager == null) 
                    return false;

                // Kiểm tra nếu target là BusinessStaff và có Supervisor là Manager đó
                var staff = await _unitOfWork.BusinessStaffRepository
                    .GetByUserIdAsync(targetUserId);

                if (staff != null && 
                    staff.SupervisorId == manager.ManagerId)
                    return true;
            }

            // Các role khác chỉ được xem bản thân
            return false;
        }
    }
}
