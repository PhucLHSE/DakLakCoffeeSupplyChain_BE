using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class WarehouseMapper
    {
        public static void MapToEntity(this WarehouseUpdateDto dto, Warehouse entity)
        {
            entity.Name = dto.Name;
            entity.Location = dto.Location;
            entity.Capacity = dto.Capacity;
            entity.ManagerId = dto.ManagerId;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        public static WarehouseViewDetailDto ToDetailDto(this Warehouse w)
        {
            return new WarehouseViewDetailDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseCode = w.WarehouseCode,
                Name = w.Name,
                Location = w.Location,
                Capacity = w.Capacity,
                ManagerId = w.ManagerId,
                ManagerName = w.Manager?.User?.Name ?? "N/A",
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            };
        }
    }
}
