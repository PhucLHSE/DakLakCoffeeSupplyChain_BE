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
        void Update(Inventory entity);
    }

}
