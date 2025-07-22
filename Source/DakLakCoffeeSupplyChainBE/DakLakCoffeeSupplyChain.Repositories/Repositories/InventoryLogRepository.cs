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
                .Include(log => log.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.CropSeason)
                .Include(log => log.Inventory)
                    .ThenInclude(i => i.Batch)
                        .ThenInclude(b => b.Farmer)
                .OrderByDescending(log => log.LoggedAt)
                .ToListAsync();
        }
    }
}
