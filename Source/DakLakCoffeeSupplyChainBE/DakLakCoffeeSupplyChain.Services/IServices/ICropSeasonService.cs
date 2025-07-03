using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropSeasonService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetAllByUserId(Guid userId);
        Task<IServiceResult> GetById(Guid cropSeasonId);
        Task<IServiceResult> Create(CropSeasonCreateDto dto, Guid userId);
        Task<IServiceResult> Update(CropSeasonUpdateDto dto);
        Task<IServiceResult> DeleteById(Guid cropSeasonId);
        Task<IServiceResult> SoftDeleteAsync(Guid cropSeasonId);
    }
}
