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
        public WarehouseReceiptRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<string> GenerateReceiptCodeAsync()
        {
            var count = await _context.WarehouseReceipts.CountAsync();
            var code = $"REC-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";
            return code;
        }
        public async Task<List<WarehouseReceipt>> GetAllWithIncludesAsync()
        {
            return await _context.WarehouseReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.ReceivedByNavigation)
                    .ThenInclude(bs => bs.User) // 👈 đúng chỗ bị lỗi lúc trước
                .OrderByDescending(r => r.ReceivedAt)
                .ToListAsync();
        }

    }
}
