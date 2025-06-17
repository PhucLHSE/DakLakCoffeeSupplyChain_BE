using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropSeasonService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid id);

        Task<IServiceResult> Create(CropSeasonCreateDto dto);

        Task<IServiceResult> Update(CropSeasonUpdateDto dto);

        Task<IServiceResult> DeleteById(Guid cropSeasonId);
    }
}
