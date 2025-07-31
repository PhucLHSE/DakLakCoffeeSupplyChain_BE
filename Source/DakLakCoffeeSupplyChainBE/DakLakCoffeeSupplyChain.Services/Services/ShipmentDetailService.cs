using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ShipmentDetailService : IShipmentDetailService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShipmentDetailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> SoftDeleteShipmentDetailById(Guid shipmentDetailId)
        {
            try
            {
                // Tìm ShipmentDetail theo ID
                var shipmentDetail = await _unitOfWork.ShipmentDetailRepository.GetByIdAsync(
                    predicate: sd =>
                       sd.ShipmentDetailId == shipmentDetailId &&
                       !sd.IsDeleted,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (shipmentDetail == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu shipmentDetail là đã xóa
                    shipmentDetail.IsDeleted = true;
                    shipmentDetail.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm shipmentDetail ở repository
                    await _unitOfWork.ShipmentDetailRepository.UpdateAsync(shipmentDetail);

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
