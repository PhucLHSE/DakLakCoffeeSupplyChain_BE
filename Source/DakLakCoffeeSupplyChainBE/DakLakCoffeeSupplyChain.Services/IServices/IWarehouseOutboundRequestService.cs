using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWarehouseOutboundRequestService
    {
        Task<IServiceResult> CreateRequestAsync(Guid managerUserId, WarehouseOutboundRequestCreateDto dto);
        Task<IServiceResult> GetDetailAsync(Guid outboundRequestId);
        Task<IServiceResult> GetAllAsync(Guid userId);
        Task<IServiceResult> AcceptRequestAsync(Guid requestId, Guid staffUserId);
    }

}
