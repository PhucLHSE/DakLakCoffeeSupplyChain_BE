using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseInboundRequestMapper
    {
        public static WarehouseInboundRequestViewDto ToViewDto(this WarehouseInboundRequest request)
        {
            return new WarehouseInboundRequestViewDto
            {
                InboundRequestId = request.InboundRequestId,
                RequestCode = request.InboundRequestCode,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                Note = request.Note,
                FarmerName = request.Farmer?.User?.Name ?? "N/A",
                BusinessStaffName = request.BusinessStaff?.User?.Name?? "N/A",
                BatchId = request.BatchId,
                BatchCode = request.Batch?.BatchCode
            };
        }

    }
}
