using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IInventoryRepository : IGenericRepository<Inventory>
    {
        Task<Inventory?> FindByWarehouseAndBatchAsync(Guid warehouseId, Guid batchId);
        Task<Inventory?> FindByIdAsync(Guid id);
        void Update(Inventory entity);
        Task<List<Inventory>> GetAllWithIncludesAsync();
        Task<Inventory?> GetDetailByIdAsync(Guid id);
        Task<int> CountCreatedInYearAsync(int year);
    }
}

