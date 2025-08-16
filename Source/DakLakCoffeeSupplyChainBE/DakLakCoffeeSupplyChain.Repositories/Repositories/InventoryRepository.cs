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
    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<Inventory?> FindByWarehouseAndBatchAsync(
            Guid warehouseId, Guid batchId)
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(inv =>
                    inv.WarehouseId == warehouseId &&
                    inv.BatchId == batchId &&
                    !inv.IsDeleted);
        }

        public async Task<Inventory?> FindByWarehouseAndDetailAsync(
            Guid warehouseId, Guid detailId)
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(inv =>
                    inv.WarehouseId == warehouseId &&
                    inv.DetailId == detailId &&
                    !inv.IsDeleted);
        }

        public async Task<Inventory?> FindByIdAsync(Guid id)
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(i => i.InventoryId == id && !i.IsDeleted);
        }

        public async Task<List<Inventory>> GetAllWithIncludesAsync(
            Expression<Func<Inventory, bool>> predicate)
        {
            return await _context.Inventories
                .Where(predicate)
                .Include(i => i.Warehouse)
                .Include(i => i.Batch)
                    .ThenInclude(b => b.CoffeeType) // Lấy loại cà phê
                .Include(i => i.Batch)
                    .ThenInclude(b => b.Products)   //  Nếu vẫn cần productName
                .Include(i => i.Detail)  // Thêm Detail cho cà phê tươi
                    .ThenInclude(d => d.CropSeason)
                .Include(i => i.Detail)
                    .ThenInclude(d => d.CommitmentDetail)
                        .ThenInclude(cd => cd.PlanDetail)
                            .ThenInclude(pd => pd.CoffeeType)
                .ToListAsync();
        }

        public async Task<Inventory?> GetDetailByIdAsync(Guid id)
        {
            return await _context.Inventories
                .Include(i => i.Warehouse)
                .Include(i => i.Batch)
                   .ThenInclude(b => b.CoffeeType) // Bắt buộc để lấy CoffeeTypeName
                .Include(i => i.Batch)
                   .ThenInclude(b => b.Products)   // Nếu bạn cần ProductName
                .Include(i => i.Detail)  // Thêm Detail cho cà phê tươi
                    .ThenInclude(d => d.CropSeason)
                .Include(i => i.Detail)
                    .ThenInclude(d => d.CommitmentDetail)
                        .ThenInclude(cd => cd.PlanDetail)
                            .ThenInclude(pd => pd.CoffeeType)
                .FirstOrDefaultAsync(i => 
                   i.InventoryId == id && 
                   !i.IsDeleted
                );
        }

        public async Task<int> CountCreatedInYearAsync(int year)
        {
            return await _context.Inventories
                .CountAsync(i => 
                   i.CreatedAt.Year == year && 
                   !i.IsDeleted
                );
        }

        public async Task<Inventory?> GetByIdWithWarehouseAsync(Guid id)
        {
            return await _context.Inventories
                .Include(i => i.Warehouse)
                .FirstOrDefaultAsync(i => 
                   i.InventoryId == id && 
                   !i.IsDeleted
                );
        }
    }
}


