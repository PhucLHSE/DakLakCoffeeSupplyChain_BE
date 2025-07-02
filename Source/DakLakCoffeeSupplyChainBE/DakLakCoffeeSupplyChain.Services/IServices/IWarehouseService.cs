using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWarehouseService
    {
        Task<IServiceResult> CreateAsync(WarehouseCreateDto dto, Guid userId);
        Task<IServiceResult> GetAllAsync(Guid userId);
        Task<IServiceResult> UpdateAsync(Guid id, WarehouseUpdateDto dto);
        Task<IServiceResult> DeleteAsync(Guid warehouseId);
        Task<IServiceResult> GetByIdAsync(Guid id);
        Task<IServiceResult> HardDeleteAsync(Guid warehouseId);
    }
}
