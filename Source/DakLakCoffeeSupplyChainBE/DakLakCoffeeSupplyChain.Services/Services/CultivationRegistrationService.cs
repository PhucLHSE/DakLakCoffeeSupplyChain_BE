using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CultivationRegistrationService(IUnitOfWork unitOfWork) : ICultivationRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<IServiceResult> GetAll()
        {

            var cultivationRegistrations = await _unitOfWork.CultivationRegistrationRepository.GetAllAsync(
                predicate: p => p.IsDeleted != true,
                include: p => p.Include(p => p.Farmer).ThenInclude(p => p.User),
                orderBy: p => p.OrderBy(p => p.RegistrationCode),
                asNoTracking: true);

            if (cultivationRegistrations == null || cultivationRegistrations.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CultivationRegistrationViewAllDto>()
                );
            }
            else
            {
                var cultivationRegistrationViewAllDto = cultivationRegistrations
                    .Select(c => c.MapToCultivationRegistrationViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationRegistrationViewAllDto
                );
            }
        }
    }
}
