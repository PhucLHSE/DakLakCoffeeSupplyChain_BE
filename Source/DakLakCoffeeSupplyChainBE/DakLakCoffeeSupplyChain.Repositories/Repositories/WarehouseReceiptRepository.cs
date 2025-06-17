using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class WarehouseReceiptRepository : GenericRepository<WarehouseReceipt>, IWarehouseReceiptRepository
    {
        public WarehouseReceiptRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<WarehouseReceipt?> GetByInboundRequestIdAsync(Guid inboundRequestId)
        {
            return await _context.WarehouseReceipts
                .FirstOrDefaultAsync(r => r.InboundRequestId == inboundRequestId);
        }
    }
}
