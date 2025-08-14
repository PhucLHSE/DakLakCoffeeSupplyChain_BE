using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropProgressService : ICropProgressService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        // Constants for stage codes
        private const string HARVESTING_STAGE_CODE = "harvesting";

        public CropProgressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll(Guid userId, bool isAdmin = false, bool isManager = false)
        {
            try
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
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi lấy danh sách tiến trình: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GetByCropSeasonDetailId(Guid cropSeasonDetailId, Guid userId, bool isAdmin = false, bool isManager = false)
        {
            try
            {
                List<CropProgress> progresses;
                
                if (isAdmin || isManager)
                {
                    // Admin và Manager có thể xem tất cả progress
                    progresses = await _unitOfWork.CropProgressRepository
                        .GetByCropSeasonDetailIdForManagerAsync(cropSeasonDetailId);
                }
                else
                {
                    // Farmer chỉ xem được progress của mình
                    progresses = await _unitOfWork.CropProgressRepository
                        .GetByCropSeasonDetailIdWithIncludesAsync(cropSeasonDetailId, userId);
                }

                var result = new CropProgressViewByDetailDto
                {
                    CropSeasonDetailId = cropSeasonDetailId,
                    Progresses = progresses.Select(p => p.ToViewAllDto()).ToList()
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi lấy tiến trình theo chi tiết mùa vụ: {ex.Message}");
            }
        }

        public async Task<IServiceResult> Create(CropProgressCreateDto dto, Guid userId)
        {
            try
            {
                // Validate input
                if (dto == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Dữ liệu đầu vào không hợp lệ.");

                // Validate stage
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Giai đoạn không tồn tại.");

                // Validate crop season detail
                var detail = await _unitOfWork.CultivationRegistrationRepository
                    .GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chi tiết mùa vụ không tồn tại.");

                // Validate user permission
                if (detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền ghi nhận tiến độ cho vùng trồng này.");

                // Validate progress date
                if (!dto.ProgressDate.HasValue)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Vui lòng chọn ngày ghi nhận.");

                if (dto.ProgressDate.Value > DateOnly.FromDateTime(DateHelper.NowVietnamTime()))
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");

                // Check for duplicates
                var duplicate = await _unitOfWork.CropProgressRepository.GetAllAsync(p =>
                    !p.IsDeleted &&
                    p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                    p.StageId == dto.StageId &&
                    p.ProgressDate == dto.ProgressDate.Value
                );
                if (duplicate.Any())
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tiến trình đã tồn tại với ngày và giai đoạn này.");

                // Get farmer information
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy thông tin nông dân.");

                // Handle harvesting stage validation
                if (stage.StageCode == HARVESTING_STAGE_CODE)
                {
                    if (!dto.ActualYield.HasValue || dto.ActualYield.Value <= 0)
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Vui lòng nhập sản lượng thực tế hợp lệ (> 0).");

                    // Update actual yield in crop season detail
                    detail.ActualYield = dto.ActualYield.Value;
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(detail);
                }

                // Create progress entity
                var entity = dto.MapToCropProgressCreateDto();
                entity.UpdatedBy = farmer.FarmerId;
                
                // Tự động lấy StageDescription từ CropStage.Description
                entity.StageDescription = stage.Description ?? string.Empty;

                await _unitOfWork.CropProgressRepository.CreateAsync(entity);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Tự động cập nhật StepIndex từ Stage.OrderIndex (sử dụng biến stage đã có)
                    if (stage?.OrderIndex.HasValue == true && stage.OrderIndex.Value != entity.StepIndex)
                    {
                        entity.StepIndex = stage.OrderIndex.Value;
                        await _unitOfWork.CropProgressRepository.UpdateAsync(entity);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // Auto update status của CropSeasonDetail
                    await UpdateCropSeasonDetailStatusAsync(dto.CropSeasonDetailId);

                    var created = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG,
                        created?.MapToCropProgressViewDetailsDto());
                }

                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi tạo tiến trình: {ex.Message}");
            }
        }

        public async Task<IServiceResult> Update(CropProgressUpdateDto dto, Guid userId)
        {
            try
            {
                // Validate input
                if (dto == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Dữ liệu đầu vào không hợp lệ.");

                // Get existing entity
                var entity = await _unitOfWork.CropProgressRepository.GetByIdAsync(dto.ProgressId);
                if (entity == null || entity.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình hoặc đã bị xoá.");

                // Validate crop season detail
                var detail = await _unitOfWork.CultivationRegistrationRepository.GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Chi tiết mùa vụ không tồn tại.");

                // Validate user permission
                if (detail.CropSeason?.Farmer?.UserId != userId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật tiến độ này.");

                // Validate stage
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Giai đoạn không tồn tại.");

                // Handle harvesting stage updates
                if (stage.StageCode == HARVESTING_STAGE_CODE && dto.ActualYield.HasValue && dto.ActualYield.Value > 0)
                {
                    detail.ActualYield = dto.ActualYield.Value;
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(detail);
                }

                // Check linked reports constraint
                var linkedReports = await _unitOfWork.GeneralFarmerReportRepository.GetAllAsync(
                    r => r.ProcessingProgressId == dto.ProgressId
                );
                if (linkedReports.Any() && dto.CropSeasonDetailId != entity.CropSeasonDetailId)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không được thay đổi vùng trồng đã liên kết báo cáo.");

                // Check for duplicates
                var duplicates = await _unitOfWork.CropProgressRepository.GetAllAsync(
                    p => p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                         p.StageId == dto.StageId &&
                         p.ProgressDate == dto.ProgressDate &&
                         p.ProgressId != dto.ProgressId &&
                         !p.IsDeleted
                );
                if (duplicates.Any())
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Đã có tiến trình khác cùng ngày và giai đoạn.");

                // Get farmer information
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy thông tin nông dân.");

                // Update entity
                dto.MapToUpdateCropProgress(entity, farmer.FarmerId);
                
                // Tự động cập nhật StageDescription từ CropStage.Description mới
                entity.StageDescription = stage.Description ?? string.Empty;
                
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
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi cập nhật tiến trình: {ex.Message}");
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

                // Handle harvesting stage deletion
                if (progress.Stage?.StageCode == HARVESTING_STAGE_CODE)
                {
                    progress.CropSeasonDetail!.ActualYield = null;
                    progress.CropSeasonDetail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(progress.CropSeasonDetail);
                }

                await _unitOfWork.CropProgressRepository.RemoveAsync(progress);
                var result = await _unitOfWork.SaveChangesAsync();

                return result > 0
                    ? new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG)
                    : new ServiceResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi xoá tiến trình: {ex.Message}");
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

                // Handle harvesting stage soft deletion
                if (progress.Stage?.StageCode == HARVESTING_STAGE_CODE)
                {
                    progress.CropSeasonDetail!.ActualYield = null;
                    progress.CropSeasonDetail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(progress.CropSeasonDetail);
                }

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
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi xoá mềm tiến trình: {ex.Message}");
            }
        }

        private async Task UpdateCropSeasonDetailStatusAsync(Guid cropSeasonDetailId)
        {
            try
            {
                var detail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == cropSeasonDetailId && !d.IsDeleted,
                    asNoTracking: false
                );

                if (detail == null) return;

                // Parse current status
                if (!Enum.TryParse<CropDetailStatus>(detail.Status, out var currentStatus))
                {
                    currentStatus = CropDetailStatus.Planned;
                }

                // Update status based on current state
                if (currentStatus == CropDetailStatus.Planned)
                {
                    // First progress created -> move to InProgress
                    detail.Status = CropDetailStatus.InProgress.ToString();
                    detail.UpdatedAt = DateHelper.NowVietnamTime();
                    await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(detail);
                    await _unitOfWork.SaveChangesAsync();
                }
                else if (currentStatus == CropDetailStatus.InProgress)
                {
                    // Check if harvesting progress exists and has actual yield
                    var harvestProgress = await _unitOfWork.CropProgressRepository.GetAllAsync(
                        predicate: p => p.CropSeasonDetailId == cropSeasonDetailId && 
                                       !p.IsDeleted && 
                                       p.Stage.StageCode == HARVESTING_STAGE_CODE,
                        include: q => q.Include(p => p.Stage),
                        asNoTracking: true
                    );

                    if (harvestProgress.Any() && detail.ActualYield.HasValue && detail.ActualYield.Value > 0)
                    {
                        detail.Status = CropDetailStatus.Completed.ToString();
                        detail.UpdatedAt = DateHelper.NowVietnamTime();
                        await _unitOfWork.CropSeasonDetailRepository.UpdateAsync(detail);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid affecting progress creation
                // TODO: Replace with proper logging framework
                Console.WriteLine($"Error updating CropSeasonDetail status: {ex.Message}");
            }
        }

        public async Task UpdateMediaUrlsAsync(Guid progressId, string? photoUrl, string? videoUrl)
        {
            try
            {
                var progress = await _unitOfWork.CropProgressRepository.GetByIdAsync(progressId);
                if (progress == null) return;

                progress.PhotoUrl = photoUrl;
                progress.VideoUrl = videoUrl;
                progress.UpdatedAt = DateHelper.NowVietnamTime();

                await _unitOfWork.CropProgressRepository.UpdateAsync(progress);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // TODO: Replace with proper logging framework
                Console.WriteLine($"Error updating media URLs: {ex.Message}");
                throw; // Re-throw as this is a public method
            }
        }
    }
}

