using CoffeeManagement.Flow4.Repositories.DBContext;
using CoffeeManagement.Flow4.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeManagement.Flow4.Repositories
{
    public interface IWarehouseInboundRequestRepository
    {
        Task<WarehouseInboundRequest> CreateAsync(WarehouseInboundRequest request);
        Task<WarehouseInboundRequest?> GetByIdAsync(Guid id);
        Task<IEnumerable<WarehouseInboundRequest>> GetByFarmerIdAsync(Guid farmerId);
    }
    public class WarehouseInboundRequestRepository : IWarehouseInboundRequestRepository
    {
        private readonly CoffeeManagementContext _context;

        public WarehouseInboundRequestRepository(CoffeeManagementContext context)
        {
            _context = context;
        }

        public async Task<WarehouseInboundRequest> CreateAsync(WarehouseInboundRequest request)
        {
            _context.WarehouseInboundRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<WarehouseInboundRequest?> GetByIdAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests.FindAsync(id);
        }

        public async Task<IEnumerable<WarehouseInboundRequest>> GetByFarmerIdAsync(Guid farmerId)
        {
            return await _context.WarehouseInboundRequests
                .Where(r => r.FarmerId == farmerId)
                .ToListAsync();
        }
    }
}