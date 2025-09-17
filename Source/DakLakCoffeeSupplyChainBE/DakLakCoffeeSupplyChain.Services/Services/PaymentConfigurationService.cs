using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
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

        public async Task<IServiceResult> Create(PaymentConfigurationCreateDto paymentConfigurationCreateDto)
        {
            try
            {
                // Kiểm tra Role có tồn tại không
                var role = await _unitOfWork.RoleRepository
                    .GetByIdAsync(paymentConfigurationCreateDto.RoleId);

                if (role == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Vai trò được chọn không tồn tại trong hệ thống."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                var newConfig = paymentConfigurationCreateDto.MapToNewPaymentConfiguration();

                // Tạo loại phí ở repository
                await _unitOfWork.PaymentConfigurationRepository
                    .CreateAsync(newConfig);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu để trả về
                    var createdConfig = await _unitOfWork.PaymentConfigurationRepository.GetByIdAsync(
                        predicate: pc => pc.ConfigId == newConfig.ConfigId,
                        include: query => query
                           .Include(pc => pc.Role),
                        asNoTracking: true
                    );

                    if (createdConfig != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdConfig.MapToPaymentConfigurationViewDetailsDto();

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

        public async Task<IServiceResult> Update(PaymentConfigurationUpdateDto paymentConfigurationUpdateDto)
        {
            try
            {
                // Tìm PaymentConfiguration theo ID
                var paymentConfiguration = await _unitOfWork.PaymentConfigurationRepository
                    .GetByIdAsync(paymentConfigurationUpdateDto.ConfigId);

                if (paymentConfiguration == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Loại phí không tồn tại hoặc đã bị xoá."
                    );
                }

                // Kiểm tra Role có tồn tại không
                var role = await _unitOfWork.RoleRepository
                    .GetByIdAsync(paymentConfigurationUpdateDto.RoleId);

                if (role == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Vai trò được chọn không tồn tại trong hệ thống."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                paymentConfigurationUpdateDto.MapToUpdatedPaymentConfiguration(paymentConfiguration);

                // Cập nhật loại phí ở repository
                await _unitOfWork.PaymentConfigurationRepository
                    .UpdateAsync(paymentConfiguration);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu sau khi update để trả về đầy đủ thông tin
                    var updatedConfig = await _unitOfWork.PaymentConfigurationRepository.GetByIdAsync(
                        predicate: pc => 
                           pc.ConfigId == paymentConfiguration.ConfigId && 
                           !pc.IsDeleted,
                        include: query => query
                           .Include(pc => pc.Role),
                        asNoTracking: true
                    );

                    if (updatedConfig != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedConfig.MapToPaymentConfigurationViewDetailsDto();

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            Const.SUCCESS_UPDATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Cập nhật thành công nhưng không thể truy xuất dữ liệu để trả về."
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
                    await _unitOfWork.PaymentConfigurationRepository
                        .UpdateAsync(paymentConfiguration);

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
