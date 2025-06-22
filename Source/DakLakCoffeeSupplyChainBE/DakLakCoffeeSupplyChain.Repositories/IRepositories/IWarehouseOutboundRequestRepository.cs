using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IWarehouseOutboundRequestRepository : IGenericRepository<WarehouseOutboundRequest>
    {
        Task<WarehouseOutboundRequest?> GetByIdAsync(Guid id);
        Task CreateAsync(WarehouseOutboundRequest entity);
        Task<List<WarehouseOutboundRequest>> GetAllAsync();
        void Update(WarehouseOutboundRequest entity);
    }
}

