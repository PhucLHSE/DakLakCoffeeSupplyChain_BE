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
    public class WarehouseInboundRequestRepository : GenericRepository<WarehouseInboundRequest>, IWarehouseInboundRequestRepository
    {
        public WarehouseInboundRequestRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<WarehouseInboundRequest?> GetByIdAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .FirstOrDefaultAsync(r => r.InboundRequestId == id && !r.IsDeleted);
        }

        public async Task<WarehouseInboundRequest?> GetByIdWithFarmerAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Farmer)
                .FirstOrDefaultAsync(r => r.InboundRequestId == id && !r.IsDeleted);
        }

        public async Task<WarehouseInboundRequest?> GetByIdWithBatchAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Batch)
                .FirstOrDefaultAsync(r => r.InboundRequestId == id && !r.IsDeleted);
        }

        public async Task<List<WarehouseInboundRequest>> GetAllPendingAsync()
        {
            return await _context.WarehouseInboundRequests
                .Where(r => r.Status == "Pending" && !r.IsDeleted) // <-- thêm lọc IsDeleted
                .Include(r => r.Farmer)
                .ToListAsync();
        }

        public async Task<List<WarehouseInboundRequest>> GetAllWithIncludesAsync()
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Farmer).ThenInclude(f => f.User)
                .Include(r => r.BusinessStaff).ThenInclude(s => s.User)
                .Include(r => r.Batch)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<WarehouseInboundRequest?> GetDetailByIdAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Farmer).ThenInclude(f => f.User)
                .Include(r => r.BusinessStaff).ThenInclude(s => s.User)
                .Include(r => r.Batch).ThenInclude(b => b.CoffeeType)
                .Include(r => r.Batch).ThenInclude(b => b.CropSeason)
                .FirstOrDefaultAsync(r => r.InboundRequestId == id && !r.IsDeleted); // <-- đã có
        }

        public void Update(WarehouseInboundRequest entity)
        {
            _context.WarehouseInboundRequests.Update(entity);
        }

        public void Delete(WarehouseInboundRequest entity)
        {
            _context.WarehouseInboundRequests.Remove(entity);
        }
        public async Task<int> CountInboundRequestsInYearAsync(int year)
        {
            return await _context.WarehouseInboundRequests
                .CountAsync(r => r.CreatedAt.Year == year && !r.IsDeleted);
        }
        public async Task<List<WarehouseInboundRequest>> GetAllByFarmerIdAsync(Guid farmerId)
        {
            return await _context.WarehouseInboundRequests
                .Where(r => r.FarmerId == farmerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
