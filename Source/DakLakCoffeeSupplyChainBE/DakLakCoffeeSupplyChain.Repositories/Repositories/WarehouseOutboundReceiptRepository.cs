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
                .AsNoTracking()
                .FirstOrDefaultAsync(r => 
                   r.OutboundRequestId == outboundRequestId && 
                   !r.IsDeleted
                );
        }

        public async Task<List<WarehouseOutboundReceipt>> GetAllWithIncludesAsync()
        {
            return await _context.WarehouseOutboundReceipts
                .AsNoTracking()
                .Include(r => r.Warehouse)
                .Include(r => r.ExportedByNavigation)
                   .ThenInclude(u => u.User)
                .Include(r => r.Batch)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.ExportedAt)
                .ToListAsync();
        }

        public async Task<WarehouseOutboundReceipt?> GetDetailByIdAsync(Guid receiptId)
        {
            return await _context.WarehouseOutboundReceipts
                .AsNoTracking()
                .Include(r => r.Warehouse)
                .Include(r => r.ExportedByNavigation).ThenInclude(u => u.User)
                .Include(r => r.Batch)
                    .ThenInclude(b => b.CoffeeType)
                .Include(r => r.Batch)
                    .ThenInclude(b => b.ProcessingBatchEvaluations)
                .Include(r => r.Batch)
                    .ThenInclude(b => b.ProcessingBatchProgresses)
                        .ThenInclude(p => p.ProcessingParameters)
                .Include(r => r.Inventory)
                    .ThenInclude(i => i.Products)
                .Include(r => r.OutboundRequest)
                    .ThenInclude(or => or.OrderItem)
                        .ThenInclude(oi => oi.Product)
                .Include(r => r.OutboundRequest)
                    .ThenInclude(or => or.OrderItem)
                        .ThenInclude(oi => oi.Order)
                            .ThenInclude(o => o.CreatedByNavigation)
                .FirstOrDefaultAsync(r => r.OutboundReceiptId == receiptId && !r.IsDeleted);
        }

        public void Update(WarehouseOutboundReceipt receipt)
        {
            _context.WarehouseOutboundReceipts.Update(receipt);
        }

        public async Task<WarehouseOutboundReceipt?> GetByIdWithoutIncludesAsync(Guid id)
        {
            return await _context.WarehouseOutboundReceipts
                .FirstOrDefaultAsync(r => 
                   r.OutboundReceiptId == id && 
                   !r.IsDeleted
                );
        }

        public async Task<List<WarehouseOutboundReceipt>> GetByOrderItemIdAsync(Guid orderItemId)
        {
            return await _context.WarehouseOutboundReceipts
                .Include(r => r.OutboundRequest)
                .Where(r => r.OutboundRequest != null
                            && r.OutboundRequest.OrderItemId == orderItemId
                            && !r.IsDeleted)
                .ToListAsync();
        }
    }
}
