using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDtos;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        public async Task<IServiceResult> GetById(Guid configId)
        {
            // Tìm PaymentConfiguration theo ID
            var paymentConfiguration = await _unitOfWork.PaymentConfigurationRepository.GetByIdAsync(
                predicate: pc =>
                   pc.ConfigId == configId,
                include: query => query
                   .Include(o => o.Role),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy PaymentConfiguration
            if (paymentConfiguration == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new PaymentConfigurationViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var paymentConfigurationDto = paymentConfiguration.MapToPaymentConfigurationViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    paymentConfigurationDto
                );
            }
        }

        public async Task<IServiceResult> DeletePaymentConfigurationById(Guid configId)
        {
            try
            {
                // Tìm PaymentConfiguration theo ID
                var paymentConfiguration = await _unitOfWork.PaymentConfigurationRepository
                    .GetByIdAsync(configId);

                // Kiểm tra nếu không tồn tại
                if (paymentConfiguration == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa paymentConfiguration khỏi repository
                    await _unitOfWork.PaymentConfigurationRepository
                        .RemoveAsync(paymentConfiguration);

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

        public async Task<IServiceResult> SoftDeletePaymentConfigurationById(Guid configId)
        {
            try
            {
                // Tìm paymentConfiguration theo ID
                var paymentConfiguration = await _unitOfWork.PaymentConfigurationRepository
                    .GetByIdAsync(configId);

                // Kiểm tra nếu không tồn tại
                if (paymentConfiguration == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    paymentConfiguration.IsDeleted = true;
                    paymentConfiguration.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm loại phí ở repository
                    await _unitOfWork.PaymentConfigurationRepository.UpdateAsync(paymentConfiguration);

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
