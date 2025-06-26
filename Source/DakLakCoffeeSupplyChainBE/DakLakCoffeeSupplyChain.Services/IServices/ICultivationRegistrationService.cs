using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICultivationRegistrationService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetById(Guid registrationId);
        Task<IServiceResult> SoftDeleteById(Guid registrationId);
        Task<IServiceResult> DeleteById(Guid registrationId);
        Task<IServiceResult> Create(CultivationRegistrationCreateViewDto registrationDto);
    }
}
