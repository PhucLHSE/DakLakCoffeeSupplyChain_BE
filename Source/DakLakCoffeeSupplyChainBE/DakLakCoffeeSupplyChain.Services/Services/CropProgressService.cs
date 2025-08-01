﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            var progresses = await _unitOfWork.CropProgressRepository.GetAllWithIncludesAsync();

            var filtered = progresses
                .Where(p => !p.IsDeleted &&
                            p.CropSeasonDetail?.CropSeason?.Farmer?.UserId == userId)
                .Select(p => p.ToViewAllDto())
                .ToList();

            if (!filtered.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có tiến trình nào thuộc quyền của bạn.");

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, filtered);
        }

        public async Task<IServiceResult> GetByCropSeasonDetailId(Guid cropSeasonDetailId, Guid userId)
        {
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
                // 1. Lấy stage
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Giai đoạn không tồn tại.");

                // 2. Lấy chi tiết mùa vụ
                var detail = await _unitOfWork.CultivationRegistrationRepository
                    .GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chi tiết mùa vụ không tồn tại.");

                // 3. Kiểm tra quyền người dùng
                if (detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền ghi nhận tiến độ cho vùng trồng này.");

                // 4. Kiểm tra ngày ghi nhận không được lớn hơn hôm nay
                if (dto.ProgressDate.Value.Date > DateHelper.NowVietnamTime().Date)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");
                }

                // 5. Kiểm tra trùng lặp (Stage + Detail + Date)
                var duplicate = await _unitOfWork.CropProgressRepository.GetAllAsync(p =>
             !p.IsDeleted &&
             p.CropSeasonDetailId == dto.CropSeasonDetailId &&
             p.StageId == dto.StageId &&
             p.ProgressDate == DateOnly.FromDateTime(dto.ProgressDate.Value)
         );

                if (duplicate.Any())
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tiến trình đã tồn tại với ngày và giai đoạn này.");

                // 6. Lấy thông tin nông dân từ userId
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy thông tin nông dân.");

                // 7. Map DTO -> Entity
                var entity = dto.MapToCropProgressCreateDto();
                entity.UpdatedBy = farmer.FarmerId;

                // 8. Cập nhật ActualYield nếu là giai đoạn HARVESTING
                if (stage.StageCode == "HARVESTING")
                {
                    if (!dto.ActualYield.HasValue || dto.ActualYield.Value <= 0)
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Vui lòng nhập sản lượng thực tế hợp lệ (> 0).");

                    detail.ActualYield = dto.ActualYield.Value;
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(detail);
                }


                // 9. Tạo tiến trình
                await _unitOfWork.CropProgressRepository.CreateAsync(entity);

                // 10. Lưu thay đổi
                var result = await _unitOfWork.SaveChangesAsync();
                if (result > 0)
                {
                    var created = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG,
                        created?.MapToCropProgressViewDetailsDto());
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }


        public async Task<IServiceResult> Update(CropProgressUpdateDto dto, Guid userId)
        {
            try
            {
                var entity = await _unitOfWork.CropProgressRepository.GetByIdAsync(dto.ProgressId);
                if (entity == null || entity.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình hoặc đã bị xoá.");

                var detail = await _unitOfWork.CultivationRegistrationRepository.GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Chi tiết mùa vụ không tồn tại.");

                // ❗ Kiểm tra quyền
                if (detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật tiến độ này.");

                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Giai đoạn không tồn tại.");

                // ✅ Cho phép cập nhật ActualYield nếu là HARVESTING
                if (stage.StageCode == "HARVESTING" && dto.ActualYield.HasValue && dto.ActualYield.Value > 0)
                {
                    detail.ActualYield = dto.ActualYield.Value;
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(detail);
                }

                var linkedReports = await _unitOfWork.GeneralFarmerReportRepository.GetAllAsync(
                    r => r.ProcessingProgressId == dto.ProgressId
                );
                if (linkedReports.Any() && dto.CropSeasonDetailId != entity.CropSeasonDetailId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không được thay đổi vùng trồng đã liên kết báo cáo.");

                var duplicates = await _unitOfWork.CropProgressRepository.GetAllAsync(
                    p => p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                         p.StageId == dto.StageId &&
                         p.ProgressDate == dto.ProgressDate &&
                         p.ProgressId != dto.ProgressId &&
                         !p.IsDeleted
                );
                if (duplicates.Any())
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, $"Đã có tiến trình khác cùng ngày và giai đoạn.");

                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy thông tin nông dân.");

                dto.MapToUpdateCropProgress(entity, farmer.FarmerId);
                await _unitOfWork.CropProgressRepository.UpdateAsync(entity);
                var saveResult = await _unitOfWork.SaveChangesAsync();

                if (saveResult > 0)
                {
                    var updated = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG,
                        updated?.MapToCropProgressViewDetailsDto());
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi hệ thống: " + ex.Message);
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
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
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
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }
    }



}
