using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICoffeeTypeService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid typeId);
        Task<IServiceResult> SoftDeleteById(Guid typeId);
        Task<IServiceResult> DeleteById(Guid typeId);
        Task<IServiceResult> Create(CoffeeTypeCreateDto coffeeTypeDto);
        Task<IServiceResult> Update(CoffeeTypeUpdateDto coffeeTypeDto);
    }
}
