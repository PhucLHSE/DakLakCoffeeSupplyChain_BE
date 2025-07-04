using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Common.Helpers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class BusinessManagerService : IBusinessManagerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public BusinessManagerService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator 
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Lấy danh sách quản lý doanh nghiệp từ repository
            var businessManagers = await _unitOfWork.BusinessManagerRepository.GetAllAsync(
                predicate: bm => bm.IsDeleted != true,
                include: query => query
                   .Include(bm => bm.User),
                orderBy: query => query.OrderBy(bm => bm.ManagerCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (businessManagers == null || 
                !businessManagers.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<BusinessManagerViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var businessManagerDtos = businessManagers
                    .Select(businessManagers => businessManagers.MapToBusinessManagerViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessManagerDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid managerId, Guid userId, string userRole)
        {
            // Kiểm tra role
            if (userRole != "Admin" && 
                userRole != "BusinessManager")
            {
                return new ServiceResult(
                    Const.FAIL_READ_CODE,
                    "Bạn không có quyền truy cập thông tin này."
                );
            }

            // Nếu là BusinessManager thì chỉ được xem chính mình
            if (userRole == "BusinessManager")
            {
                var currentManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                // Không khớp thì chặn
                if (currentManager == null || 
                    currentManager.ManagerId != managerId)
                {
                    return new ServiceResult(
                        Const.FAIL_READ_CODE,
                        "Bạn chỉ có quyền xem thông tin của chính bạn."
                    );
                }
            }

            // Tìm quản lý doanh nghiệp theo ID
            var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: bm => bm.ManagerId == managerId,
                include: query => query
                   .Include(bm => bm.User),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy quản lý doanh nghiệp
            if (businessManager == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new BusinessManagerViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var businessManagerDto = businessManager.MapToBusinessManagerViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessManagerDto
                );
            }
        }

        public async Task<IServiceResult> Create(BusinessManagerCreateDto businessManagerDto, Guid userId)
        {
            try
            {
                // Lấy thông tin người dùng theo userId
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == userId,
                    include: query => query
                       .Include(u => u.Role),
                    asNoTracking: true
                );

                // Kiểm tra người dùng có tồn tại, chưa bị xóa và có vai trò "BusinessManager"
                if (user == null || 
                    user.IsDeleted || 
                    user.Role == null || 
                    user.Role.RoleName != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Người dùng không có quyền tạo doanh nghiệp."
                    );
                }

                // Kiểm tra xem người dùng đã là BusinessManager chưa (tránh trùng)
                var existingManager = await _unitOfWork.BusinessManagerRepository
                    .GetByUserIdAsync(userId);

                if (existingManager != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Người dùng đã là quản lý doanh nghiệp."
                    );
                }

                // Kiểm tra doanh nghiệp đã được đăng ký chưa theo mã số thuế
                var businessManagerExists = await _unitOfWork.BusinessManagerRepository
                    .GetByTaxIdAsync(businessManagerDto.TaxId);

                if (businessManagerExists != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Doanh nghiệp đã được đăng ký."
                    );
                }

                // Sinh mã định danh cho BusinessManager
                string managerCode = await _codeGenerator.GenerateManagerCodeAsync();

                // Map DTO to Entity
                var newBusinessManager = businessManagerDto.MapToNewBusinessManager(userId, managerCode);

                // Tạo quản lý doanh nghiệp ở repository
                await _unitOfWork.BusinessManagerRepository.CreateAsync(newBusinessManager);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = newBusinessManager.MapToBusinessManagerViewDetailsDto();
                    responseDto.Email = user.Email;
                    responseDto.FullName = user.Name;
                    responseDto.PhoneNumber = user.PhoneNumber;

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

        public async Task<IServiceResult> Update(BusinessManagerUpdateDto businessManagerDto)
        {
            try
            {
                // Tìm đối tượng BusinessManager theo ID
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: bm => bm.ManagerId == businessManagerDto.ManagerId,
                    include: query => query
                       .Include(bm => bm.User)
                );

                // Nếu không tìm thấy
                if (businessManager == null || 
                    businessManager.IsDeleted)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Quản lý doanh nghiệp không tồn tại hoặc đã bị xóa.."
                    );
                }

                // Nếu mã số thuế bị thay đổi thì kiểm tra trùng mã số thuế
                if (!string.IsNullOrEmpty(businessManagerDto.TaxId) &&
                    businessManager.TaxId != businessManagerDto.TaxId)
                {
                    var existedTax = await _unitOfWork.BusinessManagerRepository
                        .GetByTaxIdAsync(businessManagerDto.TaxId);

                    if (existedTax != null && existedTax.ManagerId != businessManagerDto.ManagerId)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            "Mã số thuế đã được sử dụng cho một doanh nghiệp khác."
                        );
                    }
                }

                //Map DTO to Entity
                businessManagerDto.MapToUpdateBusinessManager(businessManager);

                // Cập nhật businessManager ở repository
                await _unitOfWork.BusinessManagerRepository.UpdateAsync(businessManager);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = businessManager.MapToBusinessManagerViewDetailsDto();
                    responseDto.Email = businessManager.User.Email ?? string.Empty;
                    responseDto.FullName = businessManager.User.Name ?? string.Empty;
                    responseDto.PhoneNumber = businessManager.User.PhoneNumber ?? string.Empty;

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

        public async Task<IServiceResult> DeleteById(Guid managerId)
        {
            try
            {
                // Tìm quản lý doanh nghiệp theo ID
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: bm => bm.ManagerId == managerId,
                    include: query => query
                       .Include(bm => bm.User),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (businessManager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa quản lý doanh nghiệp khỏi repository
                    await _unitOfWork.BusinessManagerRepository.RemoveAsync(businessManager);

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

        public async Task<IServiceResult> SoftDeleteById(Guid managerId)
        {
            try
            {
                // Tìm quản lý doanh nghiệp theo ID
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: bm => bm.ManagerId == managerId,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (businessManager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    businessManager.IsDeleted = true;
                    businessManager.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm quản lý doanh nghiệp ở repository
                    await _unitOfWork.BusinessManagerRepository.UpdateAsync(businessManager);

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
