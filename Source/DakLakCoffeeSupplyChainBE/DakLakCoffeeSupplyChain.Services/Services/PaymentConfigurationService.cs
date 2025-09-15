using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDtos;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class PaymentConfigurationService : IPaymentConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentConfigurationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Lấy danh sách PaymentConfiguration từ repository
            var paymentConfigurations = await _unitOfWork.PaymentConfigurationRepository.GetAllAsync(
                include: query => query
                   .Include(pc => pc.Role),
                orderBy: query => query.OrderBy(bb => bb.EffectiveFrom),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (paymentConfigurations == null ||
                !paymentConfigurations.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<PaymentConfigurationViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var paymentConfigurationDtos = paymentConfigurations
                    .Select(paymentConfigurations => paymentConfigurations.MapToPaymentConfigurationsViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    paymentConfigurationDtos
                );
            }
        }
    }
}
