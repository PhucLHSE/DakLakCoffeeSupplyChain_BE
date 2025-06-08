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
    public class WarehouseInboundRequestRepository : GenericRepository<WarehouseInboundRequest>, IWarehouseInboundRequestRepository
    {
        public WarehouseInboundRequestRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<WarehouseInboundRequest?> GetByIdWithFarmerAsync(Guid id)
        {
            return await _context.WarehouseInboundRequests
                .Include(r => r.Farmer)
                .FirstOrDefaultAsync(r => r.InboundRequestId == id);
        }

        public async Task<List<WarehouseInboundRequest>> GetAllPendingAsync()
        {
            return await _context.WarehouseInboundRequests
                .Where(r => r.Status == "Pending")
                .Include(r => r.Farmer)
                .ToListAsync();
        }
        public void Update(WarehouseInboundRequest entity)
        {
            _context.Set<WarehouseInboundRequest>().Update(entity);
        }
    }
}
