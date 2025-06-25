using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CultivationRegistrationService(IUnitOfWork unitOfWork) : ICultivationRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<IServiceResult> DeleteById(Guid registrationId)
        {
            try
            {
                // Tìm cultivation registration theo ID từ repository
                var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: u => u.RegistrationId == registrationId,
                    asNoTracking: false
                );

                // Nếu không tìm thấy, trả về cảnh báo không có dữ liệu
                if (cultivation == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa khỏi repository
                    await _unitOfWork.CultivationRegistrationRepository.RemoveAsync(cultivation);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> GetAll()
        {

            var cultivationRegistrations = await _unitOfWork.CultivationRegistrationRepository.GetAllAsync(
                predicate: c => c.IsDeleted != true,
                include: c => c.
                Include(c => c.Farmer).ThenInclude(c => c.User),
                orderBy: c => c.OrderBy(c => c.RegistrationCode),
                asNoTracking: true);

            if (cultivationRegistrations == null || cultivationRegistrations.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CultivationRegistrationViewAllDto>()
                );
            }
            else
            {
                var cultivationRegistrationViewAllDto = cultivationRegistrations
                    .Select(c => c.MapToCultivationRegistrationViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationRegistrationViewAllDto
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid registrationId)
        {
            var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                predicate: c => c.RegistrationId == registrationId,
                include: c => c.
                Include(c => c.CultivationRegistrationsDetails).
                Include(c => c.Farmer).
                ThenInclude(c => c.User),
                asNoTracking: true
                );

            if (cultivation == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new CultivationRegistrationViewSumaryDto()
                );
            }
            else
            {
                var cultivationDto = cultivation.MapToCultivationRegistrationViewSumaryDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationDto
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid registrationId)
        {
            try
            {
                // Tìm cultivation registration theo ID từ repository
                var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                    predicate: u => u.RegistrationId == registrationId,
                    asNoTracking: false
                );

                // Nếu không tìm thấy cultivation registration, trả về cảnh báo không có dữ liệu
                if (cultivation == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    cultivation.IsDeleted = true;
                    cultivation.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm ở repository
                    await _unitOfWork.CultivationRegistrationRepository.UpdateAsync(cultivation);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
