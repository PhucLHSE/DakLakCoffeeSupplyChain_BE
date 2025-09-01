using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class FarmerService(IUnitOfWork unitOfWork) : IFarmerService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public async Task<IServiceResult> GetAll()
        {
            var farmers = await _unitOfWork.FarmerRepository.GetAllAsync(
                predicate: f => f.IsDeleted != true,
                include: f => f.Include( f => f.User),
                orderBy: f => f.OrderBy(f => f.FarmerCode),
                asNoTracking: true
            );

            if (farmers == null || farmers.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<FarmerViewAllDto>()
                );
            }
            else
            {
                var farmerDtos = farmers
                    .Select(f => f.MapToFarmerViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    farmerDtos
                );
            }
        }
        public async Task<IServiceResult> GetById(Guid farmerId)
        {
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: p => p.FarmerId == farmerId,
                include: p => p.Include(p => p.User),
                asNoTracking: true
                );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new FarmerViewDetailsDto()
                );
            }
            else
            {
                var farmerDto = farmer.MapToFarmerViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    farmerDto
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid farmerId)
        {
            try
            {
                // Tìm Farmer theo ID từ repository
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    predicate: u => u.FarmerId == farmerId,
                    asNoTracking: false
                );

                // Nếu không tìm thấy farmer, trả về cảnh báo không có dữ liệu
                if (farmer == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    farmer.IsDeleted = true;
                    farmer.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm ở repository
                    await _unitOfWork.FarmerRepository.UpdateAsync(farmer);

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
        public async Task<IServiceResult> DeleteById(Guid farmerId)
        {
            try
            {
                // Tìm farmer theo ID từ repository
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    predicate: u => u.FarmerId == farmerId,
                    asNoTracking: false
                );

                // Nếu không tìm thấy, trả về cảnh báo không có dữ liệu
                if (farmer == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa khỏi repository
                    await _unitOfWork.FarmerRepository.RemoveAsync(farmer);

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

        public async Task<IServiceResult> VerifyFarmer(Guid farmerId, bool isVerified)
        {
            try
            {
                // Tìm farmer theo ID
                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                    predicate: f => f.FarmerId == farmerId,
                    asNoTracking: false
                );

                if (farmer == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy nông dân."
                    );
                }

                // Cập nhật trạng thái xác thực
                farmer.IsVerified = isVerified;
                farmer.UpdatedAt = DateHelper.NowVietnamTime();

                // Lưu thay đổi
                await _unitOfWork.FarmerRepository.UpdateAsync(farmer);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        "Cập nhật trạng thái xác thực thành công."
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Cập nhật trạng thái xác thực thất bại."
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
    }
}
