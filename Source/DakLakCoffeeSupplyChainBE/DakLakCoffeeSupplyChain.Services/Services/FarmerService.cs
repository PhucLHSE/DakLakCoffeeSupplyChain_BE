using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class FarmerService(IUnitOfWork unitOfWork) : IFarmerService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        public async Task<IServiceResult> GetAll()
        {
            var farmers = await _unitOfWork.FarmerRepository.GetAllAsync(
                predicate: f => f.IsDeleted != true,
                include: f => f.Include( f => f.User),
                orderBy: f => f.OrderBy(f => f.FarmerCode),
                asNoTracking: true
            );

            if (farmers == null || farmers.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<FarmerViewAllDto>()
                );
            }
            else
            {
                var farmerDtos = farmers
                    .Select(f => f.MapToFarmerViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    farmerDtos
                );
            }
        }
        public async Task<IServiceResult> GetById(Guid farmerId)
        {
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: p => p.FarmerId == farmerId,
                include: p => p.Include(p => p.User),
                asNoTracking: true
                );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new FarmerViewDetailsDto()
                );
            }
            else
            {
                var farmerDto = farmer.MapToFarmerViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    farmerDto
                );
            }
        }
    }
}
