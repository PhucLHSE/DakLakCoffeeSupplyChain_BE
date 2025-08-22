using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class InventoryLogRepository : GenericRepository<InventoryLog>, IInventoryLogRepository
    {
        public InventoryLogRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<IEnumerable<InventoryLog>> GetByInventoryIdAsync(Guid inventoryId)
        {
            return await _context.InventoryLogs
                .Where(l => l.InventoryId == inventoryId && !l.IsDeleted)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Warehouse)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CoffeeType)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CropSeason)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.Farmer)
                .OrderByDescending(l => l.LoggedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryLog>> GetAllAsync()
        {
            return await _context.InventoryLogs
                .Where(log => !log.IsDeleted)
                .Include(log => log.Inventory)
                    .ThenInclude(i => i.Warehouse)
                .Include(log => log.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CoffeeType)
                .OrderByDescending(log => log.LoggedAt)
                .Take(100) // ✅ Giới hạn chỉ lấy 100 log gần nhất để cải thiện performance
                .ToListAsync();
        }

        // ✅ Thêm method mới với pagination
        public async Task<IEnumerable<InventoryLog>> GetAllWithPaginationAsync(int page = 1, int pageSize = 20)
        {
            return await _context.InventoryLogs
                .Where(log => !log.IsDeleted)
                .Include(log => log.Inventory)
                    .ThenInclude(i => i.Warehouse)
                .Include(log => log.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CoffeeType)
                .OrderByDescending(log => log.LoggedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // ✅ Thêm method để đếm tổng số logs
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.InventoryLogs
                .Where(log => !log.IsDeleted)
                .CountAsync();
        }

        public async Task<InventoryLog?> GetByIdWithAllRelationsAsync(Guid logId)
        {
            return await _context.InventoryLogs
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Warehouse)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.Products) // Thêm dòng đúng này
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CoffeeType)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CropSeason)
                .Include(l => l.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.Farmer)
                            .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(l => l.LogId == logId);
        }
    }
}
