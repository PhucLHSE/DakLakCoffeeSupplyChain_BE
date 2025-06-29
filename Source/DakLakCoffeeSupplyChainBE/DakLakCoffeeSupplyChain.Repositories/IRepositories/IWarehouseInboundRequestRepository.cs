using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IWarehouseInboundRequestRepository : IGenericRepository<WarehouseInboundRequest>
    {
        Task<WarehouseInboundRequest?> GetByIdAsync(Guid id);
        Task<WarehouseInboundRequest?> GetByIdWithFarmerAsync(Guid id);
        Task<WarehouseInboundRequest?> GetByIdWithBatchAsync(Guid id);
        Task<List<WarehouseInboundRequest>> GetAllPendingAsync();
        Task<List<WarehouseInboundRequest>> GetAllWithIncludesAsync();
        Task<WarehouseInboundRequest?> GetDetailByIdAsync(Guid id);

        void Update(WarehouseInboundRequest entity);
        void Delete(WarehouseInboundRequest entity);
        Task<int> CountInboundRequestsInYearAsync(int year);
    }
}
