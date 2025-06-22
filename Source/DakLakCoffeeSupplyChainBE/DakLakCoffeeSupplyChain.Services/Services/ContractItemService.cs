using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Services.IServices;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractItemService : IContractItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContractItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> SoftDeleteContractItemById(Guid contractItemId)
        {
            try
            {
                // Tìm contractItem theo ID
                var contractItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                    predicate: ct => ct.ContractItemId == contractItemId && !ct.IsDeleted,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu contractItem là đã xóa
                    contractItem.IsDeleted = true;
                    contractItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm contractItem ở repository
                    await _unitOfWork.ContractItemRepository.UpdateAsync(contractItem);

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
