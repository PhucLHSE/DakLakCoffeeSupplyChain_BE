using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropService
    {
        Task<IServiceResult> GetAll(Guid userId);
        Task<IServiceResult> GetById(Guid cropId, Guid userId);
        Task<IServiceResult> Create(CropCreateDto dto, Guid userId);
        Task<IServiceResult> Update(CropUpdateDto dto, Guid userId);
        Task<IServiceResult> Delete(Guid cropId, Guid userId);
    }
}
