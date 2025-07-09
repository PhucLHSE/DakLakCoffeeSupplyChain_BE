using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> SoftDeleteOrderItemById(Guid orderItemId)
        {
            try
            {
                // Tìm OrderItem theo ID
                var orderItem = await _unitOfWork.OrderItemRepository.GetByIdAsync(
                    predicate: oi => 
                       oi.OrderItemId == orderItemId && 
                       !oi.IsDeleted,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (orderItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu orderItem là đã xóa
                    orderItem.IsDeleted = true;
                    orderItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm orderItem ở repository
                    await _unitOfWork.OrderItemRepository.UpdateAsync(orderItem);

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
