using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IWarehouseRepository : IGenericRepository<Warehouse>
    {
        Task<bool> IsNameExistsAsync(string name);
        Task<IEnumerable<Warehouse>> FindAsync(Expression<Func<Warehouse, bool>> predicate);
        Task<Warehouse?> GetByIdAsync(Guid id);
        void Update(Warehouse entity);
        Task<bool> HasDependenciesAsync(Guid warehouseId);
        Task<Warehouse?> GetDeletableByIdAsync(Guid warehouseId);
        Task<Warehouse?> GetByIdWithManagerAsync(Guid id);
    }
}
