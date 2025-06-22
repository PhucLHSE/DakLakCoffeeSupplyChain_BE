using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class WarehouseOutboundReceiptRepository : GenericRepository<WarehouseOutboundReceipt>, IWarehouseOutboundReceiptRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public WarehouseOutboundReceiptRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }

        public async Task<WarehouseOutboundReceipt?> GetByOutboundRequestIdAsync(Guid outboundRequestId)
        {
            return await _context.WarehouseOutboundReceipts
                .FirstOrDefaultAsync(r => r.OutboundRequestId == outboundRequestId && !r.IsDeleted);
        }
    }
}
