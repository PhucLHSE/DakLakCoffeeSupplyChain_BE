using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropProgressService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid id);

        Task<IServiceResult> Create(CropProgressCreateDto dto);

    }
}
