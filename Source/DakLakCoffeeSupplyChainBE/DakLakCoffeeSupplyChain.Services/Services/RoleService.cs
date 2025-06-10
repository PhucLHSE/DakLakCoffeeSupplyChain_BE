using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
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

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Lấy danh sách vai trò từ repository
            var roles = await _unitOfWork.RoleRepository.GetAllAsync();

            // Kiểm tra nếu không có dữ liệu
            if (roles == null || !roles.Any())
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
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(roleId);

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

        public async Task<IServiceResult> DeleteById(int roleId)
        {
            try
            {
                // Tìm role theo ID
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(roleId);

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
    }
}
