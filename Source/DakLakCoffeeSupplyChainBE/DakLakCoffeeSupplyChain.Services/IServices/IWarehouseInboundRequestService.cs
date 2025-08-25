using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWarehouseInboundRequestService
    {
        Task<IServiceResult> CreateRequestAsync(Guid userId, WarehouseInboundRequestCreateDto dto);

        Task<IServiceResult> ApproveRequestAsync(Guid requestId, Guid staffUserId);

        Task<IServiceResult> GetAllAsync(Guid userId);

        Task<IServiceResult> GetByIdAsync(Guid requestId);

        Task<IServiceResult> CancelRequestAsync(Guid requestId, Guid farmerUserId);

        Task<IServiceResult> RejectRequestAsync(Guid requestId, Guid staffUserId);
        Task<IServiceResult> GetAllByFarmerAsync(Guid userId);
        Task<IServiceResult> GetByIdForFarmerAsync(Guid requestId, Guid userId);
    }
}
