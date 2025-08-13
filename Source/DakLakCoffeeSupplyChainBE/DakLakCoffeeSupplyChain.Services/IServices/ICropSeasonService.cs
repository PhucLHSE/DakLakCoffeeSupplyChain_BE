using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

public interface ICropSeasonService
{
    Task<IServiceResult> GetAllByUserId(Guid userId, bool isAdmin, bool isManager);
    Task<IServiceResult> GetById(Guid cropSeasonId, Guid userId, bool isAdmin = false, bool isManager = false);
    Task<IServiceResult> Create(CropSeasonCreateDto dto, Guid userId);
    Task<IServiceResult> Update(CropSeasonUpdateDto dto, Guid userId, bool isAdmin = false);
    Task<IServiceResult> DeleteById(Guid cropSeasonId, Guid userId, bool isAdmin);
    Task<IServiceResult> SoftDeleteAsync(Guid cropSeasonId, Guid userId, bool isAdmin);
    Task AutoUpdateCropSeasonStatusAsync(Guid cropSeasonId);
}
