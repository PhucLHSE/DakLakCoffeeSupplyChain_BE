﻿

using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropStageService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(int id);

        Task<IServiceResult> Create(CropStageCreateDto dto);

        Task<IServiceResult> Update(CropStageUpdateDto dto);

        Task<IServiceResult> DeleteById(int stageId);
        Task<IServiceResult> SoftDeleteById(int stageId);



    }
}
