using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IInventoryService
    {
        Task<IServiceResult> GetAllAsync();
        Task<IServiceResult> GetByIdAsync(Guid id);
        Task<IServiceResult> CreateAsync(InventoryCreateDto dto);
    }
}
