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
    public class WarehouseInboundRequestRepository : GenericRepository<WarehouseInboundRequest>, IWarehouseInboundRequestRepository
    {
        public WarehouseInboundRequestRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<WarehouseInboundRequest?> GetByIdAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .FirstOrDefaultAsync(r => r.InboundRequestId == id);
        }

        public async Task<WarehouseInboundRequest?> GetByIdWithFarmerAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Farmer)
                .FirstOrDefaultAsync(r => r.InboundRequestId == id);
        }

        public async Task<WarehouseInboundRequest?> GetByIdWithBatchAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Batch)
                .FirstOrDefaultAsync(r => r.InboundRequestId == id);
        }

        public async Task<List<WarehouseInboundRequest>> GetAllPendingAsync()
        {
            return await _context.WarehouseInboundRequests
                .Where(r => r.Status == "Pending")
                .Include(r => r.Farmer)
                .ToListAsync();
        }
        public async Task<List<WarehouseInboundRequest>> GetAllWithIncludesAsync()
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Farmer).ThenInclude(f => f.User)
                .Include(r => r.BusinessStaff).ThenInclude(s => s.User)
                .Include(r => r.Batch)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public void Update(WarehouseInboundRequest entity)
        {
            _context.WarehouseInboundRequests.Update(entity);
        }

        public void Delete(WarehouseInboundRequest entity)
        {
            _context.WarehouseInboundRequests.Remove(entity);
        }
    }
}
