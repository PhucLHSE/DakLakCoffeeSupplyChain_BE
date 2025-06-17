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
        Task<IServiceResult> CreateRequestAsync(Guid farmerId, WarehouseInboundRequestCreateDto dto);
        Task<IServiceResult> ApproveRequestAsync(Guid requestId, Guid staffUserId);
        Task<IServiceResult> GetAllAsync();
        Task<IServiceResult> GetByIdAsync(Guid requestId);

    }
}
