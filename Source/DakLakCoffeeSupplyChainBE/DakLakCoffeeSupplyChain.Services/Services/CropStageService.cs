using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropStageDto;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropStageService : ICropStageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropStageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            try
            {
                var entities = await _unitOfWork.CropStageRepository.GetAllOrderedAsync();

                if (entities == null || !entities.Any())
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không có giai đoạn nào trong hệ thống.",
                        new List<CropStageViewAllDto>()
                    );
                }

                var dtos = entities.Select(x => x.ToViewDto()).ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    dtos
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi khi lấy danh sách giai đoạn: " + ex.Message);
            }
        }
        public async Task<IServiceResult> GetById(int id)
        {
            var entity = await _unitOfWork.CropStageRepository.GetByIdAsync(id);

            if (entity == null)
            {
                return new ServiceResult(
                    Const.FAIL_READ_CODE,
                    "Giai đoạn không tồn tại.",
                    null
                );
            }

            var dto = entity.MapToViewDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                dto
            );
        }

    }
}