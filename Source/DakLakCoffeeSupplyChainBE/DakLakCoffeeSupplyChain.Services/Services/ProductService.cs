﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using Microsoft.EntityFrameworkCore;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.Generators;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ProductService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            // Lấy ManagerId từ userId
            Guid? managerId = null;

            // Ưu tiên kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager != null)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                // Nếu không phải Manager, kiểm tra BusinessStaff
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                    predicate: s =>
                       s.UserId == userId &&
                       !s.IsDeleted,
                    asNoTracking: true
                );

                if (staff != null)
                {
                    managerId = staff.SupervisorId;
                }
            }

            if (managerId == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                );
            }

            // Lấy tất cả UserId thuộc doanh nghiệp đó (Manager + Staff)
            var allAccounts = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );

            var allowedUserIds = new List<Guid>();

            foreach (var account in allAccounts)
            {
                var isManager = await _unitOfWork.BusinessManagerRepository.AnyAsync(
                    m => m.ManagerId == managerId && 
                         m.UserId == account.UserId
                );

                var isStaff = await _unitOfWork.BusinessStaffRepository.AnyAsync(
                    s => s.SupervisorId == managerId && 
                         s.UserId == account.UserId
                );

                if (isManager || isStaff)
                {
                    allowedUserIds.Add(account.UserId);
                }
            }

            // Lấy danh sách sản phẩm từ repository
            var products = await _unitOfWork.ProductRepository.GetAllAsync(
                predicate: p =>
                   !p.IsDeleted &&
                   allowedUserIds.Contains(p.CreatedBy),
                include: query => query
                   .Include(p => p.CoffeeType)
                   .Include(p => p.Batch)
                   .Include(p => p.Inventory)
                      .ThenInclude(i => i.Warehouse),
                orderBy: query => query.OrderBy(p => p.ProductCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (products == null || 
                !products.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProductViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var productDtos = products
                    .Select(products => products.MapToProductViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    productDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid productId, Guid userId)
        {
            // Lấy ManagerId từ userId
            Guid? managerId = null;

            // Ưu tiên kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m =>
                   m.UserId == userId &&
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager != null)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                // Nếu không phải Manager, kiểm tra BusinessStaff
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                    predicate: s =>
                       s.UserId == userId &&
                       !s.IsDeleted,
                    asNoTracking: true
                );

                if (staff != null)
                {
                    managerId = staff.SupervisorId;
                }
            }

            if (managerId == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                );
            }

            // Lấy tất cả UserId thuộc doanh nghiệp đó (Manager + Staff)
            var allAccounts = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );

            var allowedUserIds = new List<Guid>();

            foreach (var account in allAccounts)
            {
                var isManager = await _unitOfWork.BusinessManagerRepository.AnyAsync(
                    m => m.ManagerId == managerId &&
                         m.UserId == account.UserId
                );

                var isStaff = await _unitOfWork.BusinessStaffRepository.AnyAsync(
                    s => s.SupervisorId == managerId &&
                         s.UserId == account.UserId
                );

                if (isManager || isStaff)
                {
                    allowedUserIds.Add(account.UserId);
                }
            }

            // Tìm sản phẩm theo ID, CreatedBy phải thuộc allowedUserIds
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                predicate: p =>
                   p.ProductId == productId &&
                   !p.IsDeleted &&
                   allowedUserIds.Contains(p.CreatedBy),
                include: query => query
                   .Include(p => p.ApprovedByNavigation)
                   .Include(p => p.CoffeeType)
                   .Include(p => p.Batch)
                   .Include(p => p.Inventory)
                      .ThenInclude(i => i.Warehouse),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy sản phẩm
            if (product == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ProductViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var productDto = product.MapToProductViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    productDto
                );
            }
        }

        public async Task<IServiceResult> Create(ProductCreateDto productCreateDto, Guid userId)
        {
            try
            {
                // Cho phép cả BusinessManager và BusinessStaff tạo sản phẩm
                Guid? managerId = null;

                // Kiểm tra chỉ BusinessManager được phép tạo sản phẩm
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu là Staff → lấy SupervisorId làm ManagerId
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s => 
                           s.UserId == userId && 
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Chỉ BusinessManager hoặc BusinessStaff mới được phép tạo sản phẩm."
                        );
                    }
                }

                // Kiểm tra loại cà phê
                var coffeeTypeExists = await _unitOfWork.CoffeeTypeRepository.AnyAsync(
                    c => c.CoffeeTypeId == productCreateDto.CoffeeTypeId && 
                         !c.IsDeleted
                );

                if (!coffeeTypeExists)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Loại cà phê không tồn tại hoặc đã bị xoá."
                    );
                }

                // Kiểm tra batch
                var batchExists = await _unitOfWork.ProcessingBatchRepository.AnyAsync(
                    b => b.BatchId == productCreateDto.BatchId && 
                         !b.IsDeleted
                );

                if (!batchExists)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Mẻ sơ chế không tồn tại hoặc đã bị xoá."
                    );
                }

                // Kiểm tra kho
                var inventoryExists = await _unitOfWork.Inventories.AnyAsync(
                    i => i.InventoryId == productCreateDto.InventoryId && 
                         !i.IsDeleted
                );

                if (!inventoryExists)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Kho không tồn tại hoặc đã bị xoá."
                    );
                }

                // Sinh mã sản phẩm tự động
                var productCode = await _codeGenerator.GenerateProductCodeAsync(managerId.Value);

                // Ánh xạ dữ liệu từ DTO vào entity
                var newProduct = productCreateDto.MapToNewProduct(productCode, userId);

                // Lưu vào DB
                await _unitOfWork.ProductRepository.CreateAsync(newProduct);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var createdProduct = await _unitOfWork.ProductRepository.GetByIdAsync(
                        predicate: p => 
                           p.ProductId == newProduct.ProductId && 
                           !p.IsDeleted,
                        include: query => query
                        .Include(p => p.CoffeeType)
                        .Include(p => p.Inventory)
                           .ThenInclude(i => i.Warehouse)
                        .Include(p => p.Batch)
                        .Include(p => p.ApprovedByNavigation),
                        asNoTracking: true
                    );

                    if (createdProduct != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdProduct.MapToProductViewDetailsDto();

                        return new ServiceResult(
                            Const.SUCCESS_CREATE_CODE,
                            Const.SUCCESS_CREATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Tạo thành công nhưng không truy xuất được dữ liệu để trả về."
                    );
                }

                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    Const.FAIL_CREATE_MSG
                );
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.Message
                );
            }
        }

        public async Task<IServiceResult> Update(ProductUpdateDto productUpdateDto, Guid userId)
        {
            try
            {
                // Lấy ManagerId từ userId
                Guid? managerId = null;

                // Ưu tiên: nếu là BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra là Staff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s => 
                           s.UserId == userId && 
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Bạn không có quyền cập nhật sản phẩm."
                    );
                }

                // Lấy tất cả UserId thuộc doanh nghiệp đó (Manager + Staff)
                var managerUsers = await _unitOfWork.BusinessManagerRepository.GetAllAsync(
                    predicate: m => 
                       m.ManagerId == managerId && 
                       !m.IsDeleted, 
                    asNoTracking: true
                );

                var staffUsers = await _unitOfWork.BusinessStaffRepository.GetAllAsync(
                    predicate: s => 
                       s.SupervisorId == managerId && 
                       !s.IsDeleted,
                    asNoTracking: true
                );

                var allowedUserIds = managerUsers
                    .Select(m => m.UserId)
                    .Concat(staffUsers.Select(s => s.UserId))
                    .Distinct()
                    .ToList();

                // Lấy sản phẩm cần cập nhật
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                    predicate: p =>
                        p.ProductId == productUpdateDto.ProductId &&
                        !p.IsDeleted,
                    include: query => query
                        .Include(p => p.CoffeeType)
                        .Include(p => p.Inventory)
                            .ThenInclude(i => i.Warehouse)
                        .Include(p => p.Batch)
                        .Include(p => p.ApprovedByNavigation),
                    asNoTracking: false
                );

                if (product == null ||
                    !allowedUserIds.Contains(product.CreatedBy))
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy sản phẩm hoặc bạn không có quyền chỉnh sửa sản phẩm này."
                    );
                }

                // Kiểm tra loại cà phê
                var coffeeTypeExists = await _unitOfWork.CoffeeTypeRepository.AnyAsync(
                    c => c.CoffeeTypeId == productUpdateDto.CoffeeTypeId && 
                         !c.IsDeleted
                );

                if (!coffeeTypeExists)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Loại cà phê không tồn tại hoặc đã bị xoá."
                    );
                }

                // Kiểm tra batch
                var batchExists = await _unitOfWork.ProcessingBatchRepository.AnyAsync(
                    b => b.BatchId == productUpdateDto.BatchId && 
                         !b.IsDeleted
                );

                if (!batchExists)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Mẻ sơ chế không tồn tại hoặc đã bị xoá."
                    );
                }

                // Kiểm tra kho
                var inventoryExists = await _unitOfWork.Inventories.AnyAsync(
                    i => i.InventoryId == productUpdateDto.InventoryId && 
                         !i.IsDeleted
                );

                if (!inventoryExists)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Kho không tồn tại hoặc đã bị xoá."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                productUpdateDto.MapToUpdateProduct(product);

                // Cập nhật product ở repository
                await _unitOfWork.ProductRepository.UpdateAsync(product);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu sau khi cập nhật
                    var updatedProduct = await _unitOfWork.ProductRepository.GetByIdAsync(
                        predicate: p =>
                            p.ProductId == product.ProductId && 
                            !p.IsDeleted,
                        include: query => query
                            .Include(p => p.CoffeeType)
                            .Include(p => p.Inventory)
                               .ThenInclude(i => i.Warehouse)
                            .Include(p => p.Batch)
                            .Include(p => p.ApprovedByNavigation),
                        asNoTracking: true
                    );

                    if (updatedProduct != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedProduct.MapToProductViewDetailsDto();

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            Const.SUCCESS_UPDATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Cập nhật thành công nhưng không truy xuất được dữ liệu."
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

        public async Task<IServiceResult> DeleteProductById(Guid productId, Guid userId)
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

                // Tìm sản phẩm theo ID
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                    predicate: p => p.ProductId == productId,
                    include: query => query
                       .Include(p => p.CoffeeType)
                       .Include(p => p.Batch)
                       .Include(p => p.Inventory)
                          .ThenInclude(i => i.Warehouse),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (product == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Kiểm tra quyền xoá
                    if (product.CreatedBy != userId) // Không phải chính người tạo
                    {
                        // Truy tìm staff có CreatedBy trùng, và nằm dưới quyền manager hiện tại
                        var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                            predicate: s =>
                                s.UserId == product.CreatedBy &&
                                s.SupervisorId == manager.ManagerId &&
                                !s.IsDeleted,
                            asNoTracking: true
                        );

                        if (staff == null)
                        {
                            return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                "Bạn không có quyền xóa sản phẩm do người khác tạo."
                            );
                        }
                    }

                    // Xóa sản phẩm khỏi repository
                    await _unitOfWork.ProductRepository.RemoveAsync(product);

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

        public async Task<IServiceResult> SoftDeleteProductById(Guid productId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager hiện tại từ userId
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

                // Tìm product theo ID
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                    predicate: p => 
                       p.ProductId == productId && 
                       !p.IsDeleted
                );


                // Kiểm tra nếu không tồn tại
                if (product == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Kiểm tra quyền xoá
                    if (product.CreatedBy != userId) // Không phải người tạo
                    {
                        var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                            predicate: s =>
                                s.UserId == product.CreatedBy &&
                                s.SupervisorId == manager.ManagerId &&
                                !s.IsDeleted,
                            asNoTracking: true
                        );

                        if (staff == null)
                        {
                            return new ServiceResult(
                                Const.WARNING_NO_DATA_CODE,
                                "Bạn không có quyền xóa mềm sản phẩm do người khác tạo."
                            );
                        }
                    }

                    // Đánh dấu xoá mềm bằng IsDeleted
                    product.IsDeleted = true;
                    product.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm vai trò ở repository
                    await _unitOfWork.ProductRepository.UpdateAsync(product);

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
