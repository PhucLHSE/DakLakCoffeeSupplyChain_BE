using CoffeeManagement.Flow4.Repositories;
using CoffeeManagement.Flow4.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeManagement.Flow4.Services
{
    public interface IWarehouseInboundRequestService
    {
        Task<WarehouseInboundRequest> CreateRequestAsync(WarehouseInboundRequest request);
        Task<WarehouseInboundRequest?> GetRequestByIdAsync(Guid id);
        Task<IEnumerable<WarehouseInboundRequest>> GetRequestsByFarmerAsync(Guid farmerId);
    }
    public class WarehouseInboundRequestService : IWarehouseInboundRequestService
    {
        private readonly IWarehouseInboundRequestRepository _repository;

        public WarehouseInboundRequestService(IWarehouseInboundRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<WarehouseInboundRequest> CreateRequestAsync(WarehouseInboundRequest request)
        {
            request.InboundRequestId = Guid.NewGuid();
            request.CreatedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.Status = "pending";

            return await _repository.CreateAsync(request);
        }

        public async Task<WarehouseInboundRequest?> GetRequestByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<WarehouseInboundRequest>> GetRequestsByFarmerAsync(Guid farmerId)
        {
            return await _repository.GetByFarmerIdAsync(farmerId);
        }
    }

}
