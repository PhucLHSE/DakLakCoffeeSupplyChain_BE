using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.Helpers.Security;
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
using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Lấy danh sách sản phẩm từ repository
            var products = await _unitOfWork.ProductRepository.GetAllAsync(
                predicate: p => p.IsDeleted != true,
                include: query => query
                   .Include(p => p.CoffeeType)
                   .Include(p => p.Batch)
                   .Include(p => p.Inventory)
                      .ThenInclude(i => i.Warehouse),
                orderBy: query => query.OrderBy(p => p.ProductCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (products == null || !products.Any())
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

        public async Task<IServiceResult> GetById(Guid productId)
        {
            // Tìm sản phẩm theo ID
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                predicate: p => p.ProductId == productId,
                include: query => query
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

        public async Task<IServiceResult> DeleteById(Guid productId)
        {
            try
            {
                // Tìm sản phẩm theo ID
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                    predicate: p => p.ProductId == productId,
                    include: query => query
                       .Include(p => p.CoffeeType)
                       .Include(p => p.Batch)
                       .Include(p => p.Inventory)
                       .ThenInclude(i => i.Warehouse),
                    asNoTracking: true
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

        public async Task<IServiceResult> SoftDeleteById(Guid productId)
        {
            try
            {
                // Tìm pruduct theo ID
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(productId);

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
                    // Đánh dấu xoá mềm bằng IsDeleted
                    product.IsDeleted = true;
                    product.UpdatedAt = DateTime.Now;

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
