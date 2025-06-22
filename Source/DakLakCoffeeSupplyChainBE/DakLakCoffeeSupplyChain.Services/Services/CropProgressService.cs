using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropProgressService : ICropProgressService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropProgressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAll()
        {
            var progresses = await _unitOfWork.CropProgressRepository.GetAllWithIncludesAsync();

            if (progresses == null || !progresses.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có tiến trình nào.", new List<CropProgressViewAllDto>());

            var dtoList = progresses.Select(p => p.ToViewAllDto()).ToList();
            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }

        public async Task<IServiceResult> GetById(Guid id)
        {
            var progress = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(id);

            if (progress == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, progress.MapToCropProgressViewDetailsDto());
        }

        public async Task<IServiceResult> Create(CropProgressCreateDto dto)
        {
            try
            {
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Giai đoạn không tồn tại.");

                var detail = await _unitOfWork.CultivationRegistrationRepository.GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chi tiết mùa vụ không tồn tại.");

                var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(dto.UpdatedBy);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Nông dân không tồn tại.");

                if (dto.ProgressDate.HasValue &&
                    dto.ProgressDate.Value.ToDateTime(new TimeOnly(0, 0)) > DateTime.UtcNow)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");
                }

                var duplicate = await _unitOfWork.CropProgressRepository.GetAllAsync(p =>
                    !p.IsDeleted &&
                    p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                    p.StageId == dto.StageId &&
                    p.ProgressDate == dto.ProgressDate
                );
                if (duplicate.Any())
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tiến trình đã tồn tại với ngày và giai đoạn này.");
                }

                var entity = dto.MapToCropProgressCreateDto();

                await _unitOfWork.CropProgressRepository.CreateAsync(entity);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, entity.MapToCropProgressViewDetailsDto());

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

    }
}