using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class WarehouseRepository : GenericRepository<Warehouse>, IWarehouseRepository
    {
        public WarehouseRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<bool> IsNameExistsAsync(string name)
        {
            return await _context.Warehouses.AnyAsync(w => w.Name == name && !w.IsDeleted);
        }
        public async Task<IEnumerable<Warehouse>> FindAsync(Expression<Func<Warehouse, bool>> predicate)
        {
            return await _context.Warehouses
                .Where(predicate)
                .ToListAsync();
        }
        public async Task<Warehouse?> GetByIdAsync(Guid id)
        {
            return await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == id && !w.IsDeleted);
        }
        public void Update(Warehouse entity)
        {
            _context.Warehouses.Update(entity);
        }
        public async Task<bool> HasDependenciesAsync(Guid warehouseId)
        {
            return await _context.Inventories.AnyAsync(i => i.WarehouseId == warehouseId && !i.IsDeleted)
                || await _context.WarehouseReceipts.AnyAsync(r => r.WarehouseId == warehouseId && !r.IsDeleted)
                || await _context.WarehouseOutboundRequests.AnyAsync(r => r.WarehouseId == warehouseId && !r.IsDeleted);
        }
        public async Task<Warehouse?> GetDeletableByIdAsync(Guid warehouseId)
        {
            return await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && !w.IsDeleted);
        }
        public async Task<Warehouse?> GetByIdWithManagerAsync(Guid id)
        {
            return await _context.Warehouses
                .Include(w => w.Manager)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(w => w.WarehouseId == id && !w.IsDeleted);
        }
        public async Task<int> CountWarehousesCreatedInYearAsync(int year)
        {
            return await _context.Warehouses
                .CountAsync(w => w.CreatedAt.Year == year && !w.IsDeleted);
        }
    }
}
