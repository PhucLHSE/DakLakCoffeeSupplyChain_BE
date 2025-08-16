using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.WarehouseInboundRequestEnums;
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
                BatchCode = request.Batch?.BatchCode,
                RequestedQuantity = request.RequestedQuantity ?? 0
            };
        }
        public static WarehouseInboundRequestDetailDto ToDetailDto(this WarehouseInboundRequest r)
        {
            return new WarehouseInboundRequestDetailDto
            {
                InboundRequestId = r.InboundRequestId,
                RequestCode = r.InboundRequestCode,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                PreferredDeliveryDate = r.PreferredDeliveryDate ?? default,
                ActualDeliveryDate = r.ActualDeliveryDate,

                RequestedQuantity = r.RequestedQuantity ?? 0,
                Note = r.Note,

                FarmerId = r.FarmerId,
                FarmerName = r.Farmer?.User?.Name ?? "N/A",
                FarmerPhone = r.Farmer?.User?.PhoneNumber ?? "N/A",

                BusinessStaffId = r.BusinessStaffId,
                BusinessStaffName = r.BusinessStaff?.User?.Name ?? "N/A",

                BatchId = r.BatchId,
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                CoffeeType = r.Batch?.CoffeeType?.TypeName ?? "N/A",
                SeasonCode = r.Batch?.CropSeason?.CropSeasonCode ?? "N/A"
            };
        }
        public static WarehouseInboundRequest ToEntityFromCreateDto(
    this WarehouseInboundRequestCreateDto dto,
    Guid farmerId,
    string inboundCode)
        {
            return new WarehouseInboundRequest
            {
                InboundRequestId = Guid.NewGuid(),
                InboundRequestCode = inboundCode,
                FarmerId = farmerId,
                BatchId = dto.BatchId ?? Guid.Empty,
                RequestedQuantity = dto.RequestedQuantity,
                PreferredDeliveryDate = dto.PreferredDeliveryDate,
                Status = InboundRequestStatus.Pending.ToString(),
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        public static WarehouseInboundRequestFarmerViewDto ToFarmerViewDto(this WarehouseInboundRequest r)
        {
            return new WarehouseInboundRequestFarmerViewDto
            {
                InboundRequestId = r.InboundRequestId,
                RequestCode = r.InboundRequestCode,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                PreferredDeliveryDate = r.PreferredDeliveryDate ?? default,
                RequestedQuantity = r.RequestedQuantity ?? 0,
                Note = r.Note,

                BatchId = r.BatchId,
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                CoffeeType = r.Batch?.CoffeeType?.TypeName ?? "N/A",
                SeasonCode = r.Batch?.CropSeason?.CropSeasonCode ?? "N/A"
            };
        }
        public static WarehouseInboundRequestFarmerDetailDto ToFarmerDetailDto(this WarehouseInboundRequest r)
        {
            return new WarehouseInboundRequestFarmerDetailDto
            {
                InboundRequestId = r.InboundRequestId,
                RequestCode = r.InboundRequestCode,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                PreferredDeliveryDate = r.PreferredDeliveryDate ?? default,
                ActualDeliveryDate = r.ActualDeliveryDate,

                RequestedQuantity = r.RequestedQuantity ?? 0,
                Note = r.Note,

                BatchId = r.BatchId ?? new Guid(),
                BatchCode = r.Batch?.BatchCode ?? "N/A",
                CoffeeType = r.Batch?.CoffeeType?.TypeName ?? "N/A",
                SeasonCode = r.Batch?.CropSeason?.CropSeasonCode ?? "N/A"
            };
        }





    }
}
