using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICultivationRegistrationService
    {
        Task<IServiceResult> GetAll();
        Task<IServiceResult> GetAllAvailable(Guid planId);
        Task<IServiceResult> GetById(Guid registrationId);
        Task<IServiceResult> SoftDeleteById(Guid registrationId);
        Task<IServiceResult> DeleteById(Guid registrationId);
        Task<IServiceResult> Create(CultivationRegistrationCreateViewDto registrationDto, Guid userId);
        Task<IServiceResult> UpdateStatus(CultivationRegistrationUpdateStatusDto dto, Guid userId, Guid registrationDetailId);
    }
}
