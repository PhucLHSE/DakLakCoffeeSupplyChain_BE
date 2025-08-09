using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropProgressService : ICropProgressService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICropSeasonDetailService _cropSeasonDetailService;

        public CropProgressService(IUnitOfWork unitOfWork, ICropSeasonDetailService cropSeasonDetailService)
        {
            _unitOfWork = unitOfWork;
            _cropSeasonDetailService = cropSeasonDetailService;
        }

        #region Helpers
        private static DateOnly TodayVN() => DateOnly.FromDateTime(DateHelper.NowVietnamTime());
        private static DateOnly? ToDateOnly(DateTime? dt) =>
            dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null;
        #endregion

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            var progresses = await _unitOfWork.CropProgressRepository.GetAllWithIncludesAsync();

            var data = progresses
                .Where(p => !p.IsDeleted &&
                            p.CropSeasonDetail?.CropSeason?.Farmer?.UserId == userId)
                .Select(p => p.ToViewAllDto())
                .ToList();

            if (data.Count == 0)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, data);
        }

        public async Task<IServiceResult> GetByCropSeasonDetailId(Guid cropSeasonDetailId, Guid userId)
        {
            // Kiểm tra quyền đọc trên vùng trồng
            var detail = await _unitOfWork.CultivationRegistrationRepository
                .GetCropSeasonDetailByIdAsync(cropSeasonDetailId);
            if (detail == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

            if (detail.CropSeason?.Farmer?.UserId != userId)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xem dữ liệu vùng trồng này.");

            var progresses = await _unitOfWork.CropProgressRepository
                .GetByCropSeasonDetailIdWithIncludesAsync(cropSeasonDetailId, userId);

            var result = new CropProgressViewByDetailDto
            {
                CropSeasonDetailId = cropSeasonDetailId,
                Progresses = progresses.Select(p => p.ToViewAllDto()).ToList()
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
        }

        public async Task<IServiceResult> Create(CropProgressCreateDto dto, Guid userId)
        {
            try
            {
                if (dto == null)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Dữ liệu không hợp lệ.");

                var progressDate = dto.ProgressDate;
                if (!progressDate.HasValue)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Vui lòng nhập ngày ghi nhận.");
                if (progressDate.Value > TodayVN())
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");

                // Tồn tại & quyền
                var detail = await _unitOfWork.CultivationRegistrationRepository
                    .GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Chi tiết mùa vụ không tồn tại.");
                if (detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền ghi nhận tiến độ cho vùng trồng này.");

                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Giai đoạn không tồn tại.");

                // Chống trùng (Detail + Stage + Date)
                var duplicate = await _unitOfWork.CropProgressRepository.GetAllAsync(p =>
                    !p.IsDeleted &&
                    p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                    p.StageId == dto.StageId &&
                    p.ProgressDate == progressDate.Value
                );
                if (duplicate.Any())
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Tiến trình đã tồn tại với ngày và giai đoạn này.");

                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy thông tin nông dân.");

                var entity = dto.MapToCropProgressCreateDto();
                entity.ProgressDate = progressDate.Value;
                entity.UpdatedBy = farmer.FarmerId;

                await _unitOfWork.CropProgressRepository.CreateAsync(entity);
                var save = await _unitOfWork.SaveChangesAsync();
                if (save <= 0)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);

                // Cập nhật trạng thái vùng trồng → InProgress
                await _cropSeasonDetailService.UpdateStatusAsync(
                    dto.CropSeasonDetailId,
                    CropDetailStatus.InProgress,
                    userId,
                    false
                );

                // Reload để trả DTO chi tiết
                var created = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG,
                    created?.MapToCropProgressViewDetailsDto());
            }
            catch
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }
        }

        public async Task<IServiceResult> Update(CropProgressUpdateDto dto, Guid userId)
        {
            try
            {
                if (dto == null)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Dữ liệu không hợp lệ.");

                // Giả định ProgressDate trong Update DTO là DateOnly
                if (dto.ProgressDate > TodayVN())
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");

                var entity = await _unitOfWork.CropProgressRepository.GetByIdAsync(dto.ProgressId);
                if (entity == null || entity.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình hoặc đã bị xoá.");

                var detail = await _unitOfWork.CultivationRegistrationRepository
                    .GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Chi tiết mùa vụ không tồn tại.");
                if (detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật tiến độ này.");

                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_READ_CODE, "Giai đoạn không tồn tại.");

                // Không cho đổi vùng trồng nếu đã liên kết báo cáo
                var linkedReports = await _unitOfWork.GeneralFarmerReportRepository.GetAllAsync(
                    r => r.ProcessingProgressId == dto.ProgressId
                );
                if (linkedReports.Any() && dto.CropSeasonDetailId != entity.CropSeasonDetailId)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không được thay đổi vùng trồng đã liên kết báo cáo.");

                // Chống trùng (trừ bản thân)
                var duplicates = await _unitOfWork.CropProgressRepository.GetAllAsync(
                    p => p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                         p.StageId == dto.StageId &&
                         p.ProgressDate == dto.ProgressDate &&
                         p.ProgressId != dto.ProgressId &&
                         !p.IsDeleted
                );
                if (duplicates.Any())
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Đã có tiến trình khác cùng ngày và giai đoạn.");

                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy thông tin nông dân.");

                // Map & save
                dto.MapToUpdateCropProgress(entity, farmer.FarmerId);
                entity.ProgressDate = dto.ProgressDate; // đảm bảo nhất quán
                await _unitOfWork.CropProgressRepository.UpdateAsync(entity);

                var save = await _unitOfWork.SaveChangesAsync();
                if (save <= 0)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, Const.FAIL_UPDATE_MSG);

                var updated = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG,
                    updated?.MapToCropProgressViewDetailsDto());
            }
            catch
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }
        }

        public async Task<IServiceResult> DeleteById(Guid progressId, Guid userId)
        {
            try
            {
                var progress = await _unitOfWork.CropProgressRepository.GetByIdWithDetailAsync(progressId);
                if (progress == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                if (progress.CropSeasonDetail?.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá tiến trình này.");

                await _unitOfWork.CropProgressRepository.RemoveAsync(progress);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid progressId, Guid userId)
        {
            try
            {
                var progress = await _unitOfWork.CropProgressRepository.GetByIdWithDetailAsync(progressId);
                if (progress == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                if (progress.CropSeasonDetail?.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá tiến trình này.");

                progress.IsDeleted = true;
                progress.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.CropProgressRepository.UpdateAsync(progress);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
            }
        }
    }
}
