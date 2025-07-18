﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Common.Helpers.Security;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class BusinessStaffService : IBusinessStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IPasswordHasher _passwordHasher;

        public BusinessStaffService(
            IUnitOfWork unitOfWork,
            ICodeGenerator codeGenerator,
            IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        public async Task<IServiceResult> Create(BusinessStaffCreateDto dto, Guid supervisorId)
        {
            try
            {
                // 1. Kiểm tra email đã tồn tại
                var existingUser = await _unitOfWork.UserAccountRepository.GetUserAccountByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Email đã được đăng ký.");
                }

                // 2. Kiểm tra số điện thoại nếu có
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    var existingPhone = await _unitOfWork.UserAccountRepository.GetUserAccountByPhoneAsync(dto.PhoneNumber);
                    if (existingPhone != null)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Số điện thoại đã được đăng ký.");
                    }
                }

                // 3. Kiểm tra supervisor (BusinessManager) có hợp lệ không
                var supervisor = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(supervisorId);
                if (supervisor == null || supervisor.IsDeleted)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Supervisor không hợp lệ.");
                }

                // 4. Lấy vai trò "BusinessStaff"
                var role = await _unitOfWork.RoleRepository.GetRoleByNameAsync("BusinessStaff");
                if (role == null)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy vai trò BusinessStaff.");
                }

                // 5. Tạo mã người dùng + hash mật khẩu
                var userCode = await _codeGenerator.GenerateUserCodeAsync();
                var passwordHash = _passwordHasher.Hash(dto.Password);

                var newUser = new UserAccount
                {
                    UserId = Guid.NewGuid(),
                    UserCode = userCode,
                    Email = dto.Email,
                    Name = dto.FullName,
                    PasswordHash = passwordHash,
                    PhoneNumber = dto.PhoneNumber,
                    RoleId = role.RoleId,
                    RegistrationDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    IsVerified = true,
                    EmailVerified = true,
                    Status = "active"
                };

                await _unitOfWork.UserAccountRepository.CreateAsync(newUser);

                // 6. Tạo mã nhân viên + BusinessStaff
                var staffCode = await _codeGenerator.GenerateStaffCodeAsync();
                var newStaff = dto.MapToNewBusinessStaff(newUser.UserId, staffCode, supervisor.ManagerId);

                await _unitOfWork.BusinessStaffRepository.CreateAsync(newStaff);

                // 7. Lưu thay đổi
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo nhân viên thành công.", newStaff.StaffId);
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo nhân viên thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
        public async Task<IServiceResult> GetByIdAsync(Guid staffId)
        {
            try
            {
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdWithUserAsync(staffId);

                if (staff == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }

                var dto = staff.MapToDetailDto();
                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    dto
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.Message
                );
            }
        }
        public async Task<IServiceResult> GetAllBySupervisorAsync(Guid userId)
        {
            try
            {
                var supervisor = await _unitOfWork.BusinessManagerRepository.FindByUserIdAsync(userId);
                if (supervisor == null || supervisor.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Supervisor không hợp lệ hoặc không tồn tại.");
                }

                var staffs = await _unitOfWork.BusinessStaffRepository.GetBySupervisorIdAsync(supervisor.ManagerId);
                if (staffs == null || !staffs.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);
                }

                var list = staffs.Select(s => s.MapToListDto()).ToList();
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, list);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> Update(BusinessStaffUpdateDto dto)
        {
            try
            {
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdWithUserAsync(dto.StaffId);
                if (staff == null || staff.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy nhân viên.");
                }

                dto.MapToUpdateBusinessStaff(staff);
                await _unitOfWork.BusinessStaffRepository.UpdateAsync(staff);

                var result = await _unitOfWork.SaveChangesAsync();
                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, staff.StaffId);
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid staffId)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(staffId);
            if (staff == null || staff.IsDeleted)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Staff not found or already deleted");

            staff.IsDeleted = true;
            staff.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.BusinessStaffRepository.UpdateAsync(staff);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Soft delete success")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Soft delete failed");
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid staffId)
        {
            var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(staffId);
            if (staff == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Staff not found");

            await _unitOfWork.BusinessStaffRepository.RemoveAsync(staff);
            var result = await _unitOfWork.SaveChangesAsync();

            return result > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Hard delete success")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Hard delete failed");
        }





    }
}
