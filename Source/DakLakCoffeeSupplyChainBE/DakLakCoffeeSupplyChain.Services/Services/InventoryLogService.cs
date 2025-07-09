using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class InventoryLogService : IInventoryLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetByInventoryIdAsync(Guid inventoryId)
        {
            var logs = await _unitOfWork.InventoryLogs.GetByInventoryIdAsync(inventoryId);
            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy log tồn kho thành công", logs);
        }
        public async Task<IServiceResult> GetAllAsync()
        {
            var logs = await _unitOfWork.InventoryLogs.GetAllAsync();

            if (logs == null || !logs.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có log tồn kho nào được tìm thấy", null);

            var result = logs.Select(log =>
            {
                var username = log.UpdatedBy.HasValue
                    ? _unitOfWork.UserAccountRepository.GetById(log.UpdatedBy.Value)?.Name
                    : "Hệ thống";
                return log.ToListItemDto(username);
            });

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy tất cả log tồn kho thành công", result);
        }
    }
}
