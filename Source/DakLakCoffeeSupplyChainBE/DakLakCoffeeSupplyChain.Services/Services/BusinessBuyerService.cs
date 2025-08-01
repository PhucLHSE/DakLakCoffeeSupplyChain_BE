﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Common.Helpers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class BusinessBuyerService : IBusinessBuyerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public BusinessBuyerService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy BusinessManager tương ứng với tài khoản."
                );
            }

            // Lấy danh sách buyer từ repository
            var businessBuyers = await _unitOfWork.BusinessBuyerRepository.GetAllAsync(
                predicate: bb => 
                   bb.IsDeleted != true && 
                   bb.CreatedBy == manager.ManagerId,
                include: query => query
                   .Include(bb => bb.CreatedByNavigation),
                orderBy: query => query.OrderBy(bb => bb.BuyerCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (businessBuyers == null || 
                !businessBuyers.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<BusinessBuyerViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var businessBuyerDtos = businessBuyers
                    .Select(businessBuyers => businessBuyers.MapToBusinessBuyerViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessBuyerDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid buyerId, Guid userId)
        {
            // Tìm BusinessManager tương ứng với userId
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId && 
                   m.IsDeleted != true,
                asNoTracking: true
            );

            if (manager == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy BusinessManager tương ứng với tài khoản."
                );
            }

            // Tìm buyer theo ID
            var businessBuyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                predicate: bb => 
                   bb.BuyerId == buyerId && 
                   bb.CreatedBy == manager.ManagerId &&
                   bb.IsDeleted != true,
                include: query => query
                   .Include(bb => bb.CreatedByNavigation),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy buyer
            if (businessBuyer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new BusinessBuyerViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var businessBuyerDto = businessBuyer.MapToBusinessBuyerViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessBuyerDto
                );
            }
        }

        public async Task<IServiceResult> Create(BusinessBuyerCreateDto businessBuyerDto, Guid userId)
        {
            try
            {
                // Tìm thông tin BusinessManager từ userId (token hiện tại)
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    include: query => query
                       .Include(bm => bm.User)
                          .ThenInclude(u => u.Role),
                    asNoTracking: true
                );

                // Nếu không tìm thấy BusinessManager hoặc User null → lỗi
                if (businessManager == null || 
                    businessManager.User == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Không tìm thấy thông tin quản lý doanh nghiệp."
                    );
                }

                var user = businessManager.User;

                // Kiểm tra User không bị xóa và có Role là BusinessManager
                if (user.IsDeleted || 
                    user.Role == null || 
                    user.Role.RoleName != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Người dùng không có quyền tạo khách doanh nghiệp."
                    );
                }

                var managerId = businessManager.ManagerId;

                // Kiểm tra trùng khách hàng
                var existingBuyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                    predicate: b => 
                       b.TaxId == businessBuyerDto.TaxId && 
                       b.CreatedBy == managerId &&
                       !b.IsDeleted,
                    asNoTracking: true
                );

                if (existingBuyer != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Khách hàng này đã tồn tại trong danh sách."
                    );
                }

                // Sinh mã định danh cho BusinessBuyer
                string buyerCode = await _codeGenerator.GenerateBuyerCodeAsync(managerId);

                // Ánh xạ dữ liệu từ DTO vào entity
                var newBusinessBuyer = businessBuyerDto.MapToNewBusinessBuyer(managerId, buyerCode);

                // Tạo Business Buyer ở repository
                await _unitOfWork.BusinessBuyerRepository.CreateAsync(newBusinessBuyer);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
                    var responseDto = newBusinessBuyer.MapToBusinessBuyerViewDetailDto();
                    responseDto.CreatedByName = businessManager.CompanyName;

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

        public async Task<IServiceResult> Update(BusinessBuyerUpdateDto businessBuyerDto, Guid userId)
        {
            try
            {
                // Truy vấn ra BusinessManager từ userId
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                        m.UserId == userId &&
                        !m.IsDeleted,
                    include: query => query
                       .Include(m => m.User),
                    asNoTracking: true
                );

                if (businessManager == null || 
                    businessManager.User == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy thông tin quản lý doanh nghiệp."
                    );
                }

                // Kiểm tra role
                if (businessManager.User.IsDeleted ||
                    businessManager.User.Role == null ||
                    businessManager.User.Role.RoleName != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Người dùng không có quyền cập nhật khách hàng doanh nghiệp."
                    );
                }

                var managerId = businessManager.ManagerId;

                // Tìm đối tượng businessBuyer theo ID
                var businessBuyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                    predicate: bb => 
                       bb.BuyerId == businessBuyerDto.BuyerId && 
                       !bb.IsDeleted,
                    include: query => query
                       .Include(bb => bb.CreatedByNavigation)
                );

                // Nếu không tìm thấy
                if (businessBuyer == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Khách hàng không tồn tại hoặc đã bị xóa."
                    );
                }

                // Kiểm tra quyền sở hữu
                if (businessBuyer.CreatedBy != managerId)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không có quyền cập nhật khách hàng không do doanh nghiệp của bạn tạo."
                    );
                }

                // Nếu TaxId bị thay đổi → kiểm tra trùng với khách hàng khác của cùng người tạo
                if (!string.IsNullOrWhiteSpace(businessBuyerDto.TaxId) &&
                    !string.Equals(businessBuyerDto.TaxId, businessBuyer.TaxId, StringComparison.OrdinalIgnoreCase))
                {
                    var existed = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                        predicate: b => 
                           b.TaxId == businessBuyerDto.TaxId &&
                           b.CreatedBy == businessBuyer.CreatedBy &&
                           !b.IsDeleted &&
                           b.BuyerId != businessBuyerDto.BuyerId,
                        asNoTracking: true
                    );

                    if (existed != null)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            "Mã số thuế đã được sử dụng cho một khách hàng khác."
                        );
                    }
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                businessBuyerDto.MapToUpdateBusinessBuyer(businessBuyer);

                // Cập nhật buyer ở repository
                await _unitOfWork.BusinessBuyerRepository.UpdateAsync(businessBuyer);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Ánh xạ thực thể đã lưu sang DTO phản hồi
                    var responseDto = businessBuyer.MapToBusinessBuyerViewDetailDto();
                    responseDto.CreatedByName = businessBuyer.CreatedByNavigation?.CompanyName ?? string.Empty;

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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> DeleteBusinessBuyerById(Guid buyerId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager theo userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

                // Tìm buyer theo ID và xác thực thuộc quyền quản lý
                var businessBuyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                    predicate: bb => 
                       bb.BuyerId == buyerId && 
                       bb.CreatedBy == manager.ManagerId && 
                       !bb.IsDeleted,
                    include: query => query
                       .Include(bb => bb.CreatedByNavigation),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (businessBuyer == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa buyer khỏi repository
                    await _unitOfWork.BusinessBuyerRepository.RemoveAsync(businessBuyer);

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

        public async Task<IServiceResult> SoftDeleteBusinessBuyerById(Guid buyerId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager theo userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                        m.UserId == userId &&
                        !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

                // Tìm buyer theo ID và xác thực quyền sở hữu
                var businessBuyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                    predicate: bb =>
                        bb.BuyerId == buyerId &&
                        bb.CreatedBy == manager.ManagerId &&
                        !bb.IsDeleted,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (businessBuyer == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    businessBuyer.IsDeleted = true;
                    businessBuyer.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm buyer ở repository
                    await _unitOfWork.BusinessBuyerRepository.UpdateAsync(businessBuyer);

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
