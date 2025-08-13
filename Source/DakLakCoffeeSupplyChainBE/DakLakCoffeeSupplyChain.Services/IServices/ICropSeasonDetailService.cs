using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropSeasonDetailService
    {
        Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false, bool isManager = false);
        Task<IServiceResult> GetById(Guid detailId, Guid userId, bool isAdmin = false, bool isManager = false);
        Task<IServiceResult> Create(CropSeasonDetailCreateDto dto, Guid userId, bool isAdmin = false);
        Task<IServiceResult> Update(CropSeasonDetailUpdateDto dto, Guid userId, bool isAdmin = false);
        Task<IServiceResult> DeleteById(Guid detailId, Guid userId, bool isAdmin = false);
        Task<IServiceResult> SoftDeleteById(Guid detailId, Guid userId, bool isAdmin = false);
        Task<IServiceResult> UpdateStatusAsync(Guid detailId, CropDetailStatus newStatus, Guid userId, bool isAdmin = false);
    }

}
