using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class AgriculturalExpertService : IAgriculturalExpertService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AgriculturalExpertService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // Lấy tất cả chuyên gia (ViewAll)
        public async Task<IServiceResult> GetAllAsync()
        {
            var experts = await _unitOfWork.AgriculturalExpertRepository.GetAllAsync(
                predicate: e => !e.IsDeleted,
                include: query => query.Include(e => e.User),
                orderBy: q => q.OrderBy(e => e.ExpertCode),
                asNoTracking: true
            );

            if (experts == null || !experts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy chuyên gia nào.",
                    new List<AgriculturalExpertViewAllDto>()
                );
            }

            var dtoList = experts
                .Select(e => e.MapToViewAllDto())
                .ToList();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dtoList
            );
        }

        // Lấy chuyên gia theo ID (ViewDetail)
        public async Task<IServiceResult> GetByIdAsync(Guid expertId)
        {
            var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                predicate: e => e.ExpertId == expertId && !e.IsDeleted,
                include: query => query.Include(e => e.User),
                asNoTracking: true
            );

            if (expert == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy chuyên gia.",
                    new AgriculturalExpertViewDetailDto()
                );
            }

            var dto = expert.MapToViewDetailDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }
    }
}
