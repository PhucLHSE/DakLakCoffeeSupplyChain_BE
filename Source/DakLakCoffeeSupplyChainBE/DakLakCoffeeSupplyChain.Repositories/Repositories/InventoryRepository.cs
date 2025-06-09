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
    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public InventoryRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddOrUpdateInventoryAsync(Guid warehouseId, Guid batchId, double addedQuantity, string unit)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId && i.BatchId == batchId);

            if (inventory != null)
            {
                // Nếu đã có tồn kho → cộng dồn
                inventory.Quantity += addedQuantity;
                inventory.UpdatedAt = DateTime.UtcNow;
                _context.Inventories.Update(inventory);
            }
            else
            {
                // Nếu chưa có → tạo mới
                var inventoryCode = $"INV-{DateTime.UtcNow:yyMMddHHmmss}";

                var newInventory = new Inventory
                {
                    InventoryId = Guid.NewGuid(),
                    InventoryCode = inventoryCode,
                    WarehouseId = warehouseId,
                    BatchId = batchId,
                    Quantity = addedQuantity,
                    Unit = unit,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Inventories.AddAsync(newInventory);
            }
        }
    }
}
