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
        public async Task<List<WarehouseReceipt>> GetAllWithIncludesAsync()
        {
            return await _context.WarehouseReceipts
                .Where(r => !r.IsDeleted)
                .Include(r => r.Warehouse)
                .Include(r => r.Batch)
                .Include(r => r.ReceivedByNavigation).ThenInclude(s => s.User)
                .OrderByDescending(r => r.ReceivedAt)
                .ToListAsync();
        }

        public async Task<WarehouseReceipt?> GetDetailByIdAsync(Guid id)
        {
            return await _context.WarehouseReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.Batch)
                .Include(r => r.ReceivedByNavigation).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.ReceiptId == id && !r.IsDeleted);
        }
        public async Task<int> CountCreatedInYearAsync(int year)
        {
            return await _context.WarehouseReceipts
                .CountAsync(r => r.ReceivedAt.HasValue && r.ReceivedAt.Value.Year == year && !r.IsDeleted);
        }
    }
}
