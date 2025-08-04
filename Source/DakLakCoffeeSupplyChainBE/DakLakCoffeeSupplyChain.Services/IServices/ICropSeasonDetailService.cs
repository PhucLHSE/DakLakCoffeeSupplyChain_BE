using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropSeasonDetailService
    {
        Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false);
        Task<IServiceResult> GetById(Guid detailId, Guid userId, bool isAdmin = false);
        Task<IServiceResult> Create(CropSeasonDetailCreateDto dto);
        Task<IServiceResult> Update(CropSeasonDetailUpdateDto dto);
        Task<IServiceResult> DeleteById(Guid detailId);
        Task<IServiceResult> SoftDeleteById(Guid detailId);
        Task<IServiceResult> UpdateStatusAsync(Guid detailId, CropDetailStatus newStatus, Guid userId, bool isAdmin = false);
    }
}
