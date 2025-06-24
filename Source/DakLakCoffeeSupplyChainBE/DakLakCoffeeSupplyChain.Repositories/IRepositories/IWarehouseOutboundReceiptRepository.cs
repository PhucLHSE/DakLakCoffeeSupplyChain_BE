using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IWarehouseOutboundReceiptRepository : IGenericRepository<WarehouseOutboundReceipt>
    {
        Task<WarehouseOutboundReceipt?> GetByOutboundRequestIdAsync(Guid outboundRequestId);
        Task<List<WarehouseOutboundReceipt>> GetAllWithIncludesAsync();
        Task<WarehouseOutboundReceipt?> GetDetailByIdAsync(Guid receiptId);


    }
}
