using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IInventoryLogService
    {
        Task<IServiceResult> GetByInventoryIdAsync(Guid inventoryId);
        Task<IServiceResult> GetAllAsync(Guid userId);
        Task<IServiceResult> GetLogsByInventoryIdAsync(Guid inventoryId, Guid userId);

    }
}
