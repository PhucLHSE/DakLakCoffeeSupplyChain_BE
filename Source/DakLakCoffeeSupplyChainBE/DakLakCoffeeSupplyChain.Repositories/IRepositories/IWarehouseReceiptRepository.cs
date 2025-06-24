using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IWarehouseReceiptRepository : IGenericRepository<WarehouseReceipt>
    {
        Task<WarehouseReceipt?> GetByInboundRequestIdAsync(Guid inboundRequestId);
        Task<List<WarehouseReceipt>> GetAllWithIncludesAsync();
        Task<WarehouseReceipt?> GetDetailByIdAsync(Guid id);
    }
}
