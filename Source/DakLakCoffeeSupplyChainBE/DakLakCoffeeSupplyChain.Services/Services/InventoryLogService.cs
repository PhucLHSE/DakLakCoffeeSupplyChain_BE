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
    }
}
