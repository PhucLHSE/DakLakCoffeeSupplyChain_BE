using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropSeasonDetailService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid detailId);

        Task<IServiceResult> Create(CropSeasonDetailCreateDto dto);
        Task<IServiceResult> Update(CropSeasonDetailUpdateDto dto);
        Task<IServiceResult> DeleteById(Guid detailId);
        Task<IServiceResult> SoftDeleteById(Guid detailId);

    }
}
