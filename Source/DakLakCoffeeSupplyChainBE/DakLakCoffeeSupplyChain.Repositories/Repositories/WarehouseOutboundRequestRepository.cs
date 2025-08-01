﻿using DakLakCoffeeSupplyChain.Repositories.Base;
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
    public class WarehouseOutboundRequestRepository : GenericRepository<WarehouseOutboundRequest>, IWarehouseOutboundRequestRepository
    {
        public WarehouseOutboundRequestRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<WarehouseOutboundRequest?> GetByIdAsync(Guid id)
        {
            return await _context.WarehouseOutboundRequests
                .AsNoTracking()
                .Include(r => r.Inventory)
                .Include(r => r.Warehouse)
            .Include(r => r.RequestedByNavigation)
                .FirstOrDefaultAsync(r => r.OutboundRequestId == id && !r.IsDeleted);
        }
        public async Task<WarehouseOutboundRequest?> GetByIdWithoutIncludesAsync(Guid id)
        {
            return await _context.WarehouseOutboundRequests
                .FirstOrDefaultAsync(r => r.OutboundRequestId == id && !r.IsDeleted);
        }

        public async Task CreateAsync(WarehouseOutboundRequest entity)
        {
            await _context.WarehouseOutboundRequests.AddAsync(entity);
        }
        public async Task<List<WarehouseOutboundRequest>> GetAllAsync()
        {
            return await _context.WarehouseOutboundRequests
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Warehouse)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        public void Update(WarehouseOutboundRequest entity)
        {
            _context.WarehouseOutboundRequests.Update(entity);
        }
        public async Task<int> CountOutboundRequestsInYearAsync(int year)
        {
            return await _context.WarehouseOutboundRequests
                .CountAsync(r => r.CreatedAt.Year == year && !r.IsDeleted);
        }
        public async Task<double> SumRequestedQuantityByOrderItemId(Guid orderItemId)
        {
            return await _context.WarehouseOutboundRequests
                .Where(r => r.OrderItemId == orderItemId && !r.IsDeleted)
                .SumAsync(r => (double?)r.RequestedQuantity) ?? 0.0;
        }
    }
}
