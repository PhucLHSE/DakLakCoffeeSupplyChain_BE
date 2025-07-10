using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProcessingWasteService
    {
        Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin);
        Task<IServiceResult> GetByIdAsync(Guid wasteId, Guid userId, bool isAdmin);
        Task<IServiceResult> CreateAsync(ProcessingWasteCreateDto dto, Guid userId, bool isAdmin);


    }
}
