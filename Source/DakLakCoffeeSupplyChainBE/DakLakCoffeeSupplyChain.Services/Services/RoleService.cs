using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Common.Enum.RoleEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Lấy danh sách vai trò từ repository
            var roles = await _unitOfWork.RoleRepository.GetAllAsync(
                predicate: role => role.IsDeleted != true
            );

            // Kiểm tra nếu không có dữ liệu
            if (roles == null || 
                !roles.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<RoleViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var roleDtos = roles
                    .Select(roles => roles.MapToRoleViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    roleDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(int roleId)
        {
            // Tìm role theo ID
            var role = await _unitOfWork.RoleRepository
                .GetByIdAsync(roleId);

            // Kiểm tra nếu không tìm thấy role
            if (role == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new RoleViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var roleDto = role.MapToRoleViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    roleDto
                );
            }
        }

        public async Task<IServiceResult> Create(RoleCreateDto roleDto)
        {
            try
            {
                // Kiểm tra role đã tồn tại chưa
                var roleExists = await _unitOfWork.RoleRepository
                    .GetRoleByNameAsync(roleDto.RoleName);

                if (roleExists != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Vai trò đã được đăng ký."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                var newRole = roleDto.MapToNewRole();

                // Tạo vai trò ở repository
                await _unitOfWork.RoleRepository.CreateAsync(newRole);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
                    var responseDto = newRole.MapToRoleViewDetailsDto();

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

        public async Task<IServiceResult> Update(RoleUpdateDto roleDto)
        {
            try
            {
                // Kiểm tra Role tồn tại
                var role = await _unitOfWork.RoleRepository
                    .GetByIdAsync(roleDto.RoleId);

                if (role == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Vai trò không tồn tại."
                    );
                }

                // Kiểm tra trùng tên (khác chính nó)
                var roleExists = await _unitOfWork.RoleRepository
                    .GetRoleByNameAsync(roleDto.RoleName);

                if (roleExists != null && 
                    roleExists.RoleId != roleDto.RoleId)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Vai trò đã được đăng ký."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                roleDto.MapToUpdateRole(role);

                // Cập nhật vai trò ở repository
                await _unitOfWork.RoleRepository.UpdateAsync(role);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
                    var responseDto = role.MapToRoleViewDetailsDto();

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

        public async Task<IServiceResult> DeleteRoleById(int roleId)
        {
            try
            {
                // Tìm role theo ID
                var role = await _unitOfWork.RoleRepository
                    .GetByIdAsync(roleId);

                // Kiểm tra nếu không tồn tại
                if (role == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa role khỏi repository
                    await _unitOfWork.RoleRepository.RemoveAsync(role);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
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
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteRoleById(int roleId)
        {
            try
            {
                // Tìm role theo ID
                var role = await _unitOfWork.RoleRepository
                    .GetByIdAsync(roleId);

                // Kiểm tra nếu không tồn tại
                if (role == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    role.IsDeleted = true;
                    role.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm vai trò ở repository
                    await _unitOfWork.RoleRepository.UpdateAsync(role);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
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
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
