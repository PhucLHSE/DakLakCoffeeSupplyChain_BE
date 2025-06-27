using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
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
    public class ContractDeliveryItemService : IContractDeliveryItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContractDeliveryItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> SoftDeleteContractDeliveryItemById(Guid deliveryItemId)
        {
            try
            {
                // Tìm contractDeliveryItem theo ID
                var contractDeliveryItem = await _unitOfWork.ContractDeliveryItemRepository.GetByIdAsync(
                    predicate: cdi => cdi.DeliveryItemId == deliveryItemId,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractDeliveryItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu ContractDeliveryItem là đã xóa
                    contractDeliveryItem.IsDeleted = true;
                    contractDeliveryItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm contractDeliveryBatch ở repository
                    await _unitOfWork.ContractDeliveryItemRepository.UpdateAsync(contractDeliveryItem);

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
