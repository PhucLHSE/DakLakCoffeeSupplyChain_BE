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

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {

            var products = await _unitOfWork.ProductRepository.GetAllProductsAsync();

            if (products == null || !products.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProductViewAllDto>()
                );
            }
            else
            {
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
            var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(productId);

            if (product == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ProductViewDetailsDto()
                );
            }
            else
            {
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

                var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(productId);

                if (product == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    await _unitOfWork.ProductRepository.RemoveAsync(product);
                    var result = await _unitOfWork.SaveChangesAsync();

                    if (result == Const.SUCCESS_DELETE_CODE)
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
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
