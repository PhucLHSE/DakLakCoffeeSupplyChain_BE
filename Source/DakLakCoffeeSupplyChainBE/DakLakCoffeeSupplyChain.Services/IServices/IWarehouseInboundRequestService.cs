using DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs;
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
        Task<IServiceResult> ApproveInboundRequestAsync(Guid requestId, Guid staffUserId);
        Task<IServiceResult> GetAllRequestsAsync();
    }
}
