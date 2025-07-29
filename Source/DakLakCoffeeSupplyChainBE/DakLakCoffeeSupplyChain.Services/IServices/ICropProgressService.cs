using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropProgressService
    {
        Task<IServiceResult> GetAll(Guid userId);
        Task<IServiceResult> GetByCropSeasonDetailId(Guid cropSeasonDetailId, Guid userId);
        Task<IServiceResult> Create(CropProgressCreateDto dto, Guid userId);
        Task<IServiceResult> Update(CropProgressUpdateDto dto, Guid userId);
        Task<IServiceResult> DeleteById(Guid progressId, Guid userId);
        Task<IServiceResult> SoftDeleteById(Guid progressId, Guid userId);
    }
}
