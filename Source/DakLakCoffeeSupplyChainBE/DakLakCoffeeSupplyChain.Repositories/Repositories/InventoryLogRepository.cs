﻿using DakLakCoffeeSupplyChain.Repositories.Base;
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
                .OrderByDescending(l => l.LoggedAt)
                .ToListAsync();
        }
    }
}
