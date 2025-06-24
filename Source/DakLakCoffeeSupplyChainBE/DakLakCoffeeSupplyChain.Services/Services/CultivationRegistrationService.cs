using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
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
                predicate: c => c.IsDeleted != true,
                include: c => c.
                Include(c => c.Farmer).ThenInclude(c => c.User),
                orderBy: c => c.OrderBy(c => c.RegistrationCode),
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

        public async Task<IServiceResult> GetById(Guid registrationId)
        {
            var cultivation = await _unitOfWork.CultivationRegistrationRepository.GetByIdAsync(
                predicate: c => c.RegistrationId == registrationId,
                include: c => c.
                Include(c => c.CultivationRegistrationsDetails).
                Include(c => c.Farmer).
                ThenInclude(c => c.User),
                asNoTracking: true
                );

            if (cultivation == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new CultivationRegistrationViewSumaryDto()
                );
            }
            else
            {
                var cultivationDto = cultivation.MapToCultivationRegistrationViewSumaryDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cultivationDto
                );
            }
        }
    }
}
