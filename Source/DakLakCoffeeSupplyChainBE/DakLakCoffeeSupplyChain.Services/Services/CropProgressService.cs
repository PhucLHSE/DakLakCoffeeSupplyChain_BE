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
        private readonly ICropSeasonService _cropSeasonService;
        
        // Constants for stage codes
        private const string HARVESTING_STAGE_CODE = "harvesting";

        public CropProgressService(IUnitOfWork unitOfWork, ICropSeasonService cropSeasonService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cropSeasonService = cropSeasonService ?? throw new ArgumentNullException(nameof(cropSeasonService));
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
                Console.WriteLine($"CropProgressService.Create called with:");
                Console.WriteLine($"  DTO: {dto?.CropSeasonDetailId}, StageId: {dto?.StageId}, ProgressDate: {dto?.ProgressDate}");
                Console.WriteLine($"  UserId: {userId}");
                
                // Validate input
                if (dto == null)
                {
                    Console.WriteLine("DTO is null");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Dữ liệu đầu vào không hợp lệ.");
                }

                // Validate stage
                Console.WriteLine($"Validating stage with ID: {dto.StageId}");
                var stage = await _unitOfWork.CropStageRepository.GetByIdAsync(dto.StageId);
                if (stage == null)
                {
                    Console.WriteLine($"Stage not found with ID: {dto.StageId}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Giai đoạn không tồn tại.");
                }
                Console.WriteLine($"Stage found: {stage.StageName} ({stage.StageCode})");

                // Validate crop season detail
                Console.WriteLine($"Validating crop season detail with ID: {dto.CropSeasonDetailId}");
                var detail = await _unitOfWork.CultivationRegistrationRepository
                    .GetCropSeasonDetailByIdAsync(dto.CropSeasonDetailId);
                if (detail == null)
                {
                    Console.WriteLine($"Crop season detail not found with ID: {dto.CropSeasonDetailId}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chi tiết mùa vụ không tồn tại.");
                }
                Console.WriteLine($"Crop season detail found: {detail.DetailId}");

                // Validate user permission
                Console.WriteLine($"Validating user permission - Detail Farmer UserId: {detail.CropSeason?.Farmer?.UserId}, Current UserId: {userId}");
                if (detail.CropSeason?.Farmer?.UserId != userId)
                {
                    Console.WriteLine($"Permission denied - User mismatch");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền ghi nhận tiến độ cho vùng trồng này.");
                }
                Console.WriteLine($"User permission validated");

                // Validate progress date
                Console.WriteLine($"Validating progress date: {dto.ProgressDate}");
                if (!dto.ProgressDate.HasValue)
                {
                    Console.WriteLine("Progress date is null");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Vui lòng chọn ngày ghi nhận.");
                }

                var today = DateOnly.FromDateTime(DateHelper.NowVietnamTime());
                Console.WriteLine($"Today: {today}, Progress Date: {dto.ProgressDate.Value}");
                
                // Không cho phép ngày trong tương lai
                if (dto.ProgressDate.Value > today)
                {
                    Console.WriteLine($"Progress date is in future: {dto.ProgressDate.Value} > {today}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được lớn hơn hôm nay.");
                }
                
                // Không cho phép ngày quá xa trong quá khứ (tối đa 1 năm)
                var minDate = today.AddDays(-365);
                if (dto.ProgressDate.Value < minDate)
                {
                    Console.WriteLine($"Progress date is too far in past: {dto.ProgressDate.Value} < {minDate}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được quá xa trong quá khứ (tối đa 1 năm trước).");
                }
                Console.WriteLine("Progress date validation passed");
                
                // Kiểm tra ngày có hợp lý với mùa vụ không
                var cropSeason = detail.CropSeason;
                Console.WriteLine($"Crop season dates - Start: {cropSeason?.StartDate}, End: {cropSeason?.EndDate}");
                if (cropSeason?.StartDate.HasValue == true && cropSeason?.EndDate.HasValue == true)
                {
                    if (dto.ProgressDate.Value < cropSeason.StartDate.Value)
                    {
                        Console.WriteLine($"Progress date before crop season start: {dto.ProgressDate.Value} < {cropSeason.StartDate.Value}");
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được trước ngày bắt đầu mùa vụ.");
                    }
                    
                    if (dto.ProgressDate.Value > cropSeason.EndDate.Value.AddDays(30))
                    {
                        Console.WriteLine($"Progress date too far after crop season end: {dto.ProgressDate.Value} > {cropSeason.EndDate.Value.AddDays(30)}");
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Ngày ghi nhận không được quá xa sau ngày kết thúc mùa vụ.");
                    }
                }
                Console.WriteLine("Crop season date validation passed");

                // Check for duplicates
                Console.WriteLine("Checking for duplicates...");
                var duplicate = await _unitOfWork.CropProgressRepository.GetAllAsync(p =>
                    !p.IsDeleted &&
                    p.CropSeasonDetailId == dto.CropSeasonDetailId &&
                    p.StageId == dto.StageId &&
                    p.ProgressDate == dto.ProgressDate.Value
                );
                if (duplicate.Any())
                {
                    Console.WriteLine($"Duplicate found - Count: {duplicate.Count()}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Tiến trình đã tồn tại với ngày và giai đoạn này.");
                }
                Console.WriteLine("No duplicates found");

                // Validate stage order - check if this is the next valid stage
                Console.WriteLine("Validating stage order...");
                var existingProgress = await _unitOfWork.CropProgressRepository
                    .GetByCropSeasonDetailIdWithIncludesAsync(dto.CropSeasonDetailId, userId);
                
                Console.WriteLine($"Existing progress count: {existingProgress.Count}");
                var nextValidStage = GetNextValidStage(existingProgress);
                Console.WriteLine($"Next valid stage: {nextValidStage?.StageName} (ID: {nextValidStage?.StageId})");
                
                if (nextValidStage != null && dto.StageId != nextValidStage.StageId)
                {
                    Console.WriteLine($"Stage order validation failed - Expected: {nextValidStage.StageId}, Actual: {dto.StageId}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, 
                        $"Vui lòng tạo tiến độ theo đúng thứ tự giai đoạn. Giai đoạn tiếp theo cần tạo là: {nextValidStage.StageName}");
                }
                Console.WriteLine("Stage order validation passed");
                
                // Validate date order - check if current progress date is after previous progress date
                Console.WriteLine("Validating date order...");
                var previousProgress = existingProgress
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.ProgressDate)
                    .FirstOrDefault();
                
                if (previousProgress != null)
                {
                    Console.WriteLine($"Previous progress date: {previousProgress.ProgressDate}, Current: {dto.ProgressDate.Value}");
                    if (dto.ProgressDate.Value <= previousProgress.ProgressDate)
                    {
                        Console.WriteLine($"Date order validation failed - Current date not after previous");
                        return new ServiceResult(Const.FAIL_CREATE_CODE, 
                            $"Ngày ghi nhận phải sau ngày của giai đoạn trước ({previousProgress.ProgressDate:dd/MM/yyyy}).");
                    }
                }
                Console.WriteLine("Date order validation passed");

                // Get farmer information
                Console.WriteLine($"Getting farmer information for userId: {userId}");
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    Console.WriteLine($"Farmer not found for userId: {userId}");
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy thông tin nông dân.");
                }
                Console.WriteLine($"Farmer found: {farmer.FarmerId}");

                // Handle harvesting stage validation and auto-update status
                Console.WriteLine($"Stage code: {stage.StageCode}, HARVESTING_STAGE_CODE: {HARVESTING_STAGE_CODE}");
                if (stage.StageCode == HARVESTING_STAGE_CODE)
                {
                    Console.WriteLine($"Validating actual yield for harvesting stage: {dto.ActualYield}");
                    if (!dto.ActualYield.HasValue || dto.ActualYield.Value <= 0)
                    {
                        Console.WriteLine($"Actual yield validation failed: {dto.ActualYield}");
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Vui lòng nhập sản lượng thực tế hợp lệ (> 0).");
                    }
                    Console.WriteLine($"Actual yield validation passed: {dto.ActualYield.Value}");

                    // Update actual yield and status in crop season detail using ExecuteUpdateAsync
                    var newStatus = detail.Status == CropDetailStatus.InProgress.ToString() 
                        ? CropDetailStatus.Completed.ToString() 
                        : detail.Status;
                    
                    await _unitOfWork.CropSeasonDetailRepository.GetAllQueryable()
                        .Where(d => d.DetailId == dto.CropSeasonDetailId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.ActualYield, (double)dto.ActualYield.Value)
                            .SetProperty(d => d.Status, newStatus)
                            .SetProperty(d => d.UpdatedAt, DateHelper.NowVietnamTime()));
                    
                    // Auto update CropSeason status
                    await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(detail.CropSeasonId);
                }

                // Create progress entity
                Console.WriteLine("Creating progress entity...");
                var entity = dto.MapToCropProgressCreateDto();
                entity.UpdatedBy = farmer.FarmerId;
                
                // Tự động lấy StageDescription từ CropStage.Description
                entity.StageDescription = stage.Description ?? string.Empty;
                Console.WriteLine($"Entity created - ProgressId: {entity.ProgressId}, StageDescription: {entity.StageDescription}");

                await _unitOfWork.CropProgressRepository.CreateAsync(entity);
                Console.WriteLine("Entity saved to repository");
                var result = await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"SaveChanges result: {result}");

                if (result > 0)
                {
                    Console.WriteLine("SaveChanges successful, proceeding with post-creation tasks...");
                    
                    // Tự động cập nhật StepIndex từ Stage.OrderIndex
                    if (stage?.OrderIndex.HasValue == true && stage.OrderIndex.Value != entity.StepIndex)
                    {
                        Console.WriteLine($"Updating StepIndex from {entity.StepIndex} to {stage.OrderIndex.Value}");
                        entity.StepIndex = stage.OrderIndex.Value;
                        await _unitOfWork.CropProgressRepository.UpdateAsync(entity);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // Auto update status của CropSeasonDetail
                    Console.WriteLine("Updating CropSeasonDetail status...");
                    await UpdateCropSeasonDetailStatusAsync(dto.CropSeasonDetailId);

                    var created = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                    Console.WriteLine($"Created progress retrieved: {created?.ProgressId}");
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG,
                        created?.MapToCropProgressViewDetailsDto());
                }

                Console.WriteLine("SaveChanges failed");
                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                Console.WriteLine($"Error in CropProgressService.Create: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                
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
                    // Update actual yield and status in crop season detail using ExecuteUpdateAsync
                    var newStatus = detail.Status == CropDetailStatus.InProgress.ToString() 
                        ? CropDetailStatus.Completed.ToString() 
                        : detail.Status;
                    
                    await _unitOfWork.CropSeasonDetailRepository.GetAllQueryable()
                        .Where(d => d.DetailId == dto.CropSeasonDetailId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.ActualYield, (double)dto.ActualYield.Value)
                            .SetProperty(d => d.Status, newStatus)
                            .SetProperty(d => d.UpdatedAt, DateHelper.NowVietnamTime()));
                    
                    // Auto update CropSeason status
                    await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(detail.CropSeasonId);
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
                    // Auto update status của CropSeasonDetail
                    await UpdateCropSeasonDetailStatusAsync(dto.CropSeasonDetailId);
                    
                    var updated = await _unitOfWork.CropProgressRepository.GetByIdWithIncludesAsync(entity.ProgressId);
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG,
                        updated?.MapToCropProgressViewDetailsDto());
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại.");
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                Console.WriteLine($"Error in CropProgressService.Update: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
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
                    // Reset actual yield and status using ExecuteUpdateAsync
                    var newStatus = progress.CropSeasonDetail!.Status == CropDetailStatus.Completed.ToString() 
                        ? CropDetailStatus.InProgress.ToString() 
                        : progress.CropSeasonDetail.Status;
                    
                    await _unitOfWork.CropSeasonDetailRepository.GetAllQueryable()
                        .Where(d => d.DetailId == progress.CropSeasonDetail!.DetailId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.ActualYield, (double?)null)
                            .SetProperty(d => d.Status, newStatus)
                            .SetProperty(d => d.UpdatedAt, DateHelper.NowVietnamTime()));
                    
                    // Auto update CropSeason status
                    await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(progress.CropSeasonDetail.CropSeasonId);
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
                    // Reset actual yield and status using ExecuteUpdateAsync
                    var newStatus = progress.CropSeasonDetail!.Status == CropDetailStatus.Completed.ToString() 
                        ? CropDetailStatus.InProgress.ToString() 
                        : progress.CropSeasonDetail.Status;
                    
                    await _unitOfWork.CropSeasonDetailRepository.GetAllQueryable()
                        .Where(d => d.DetailId == progress.CropSeasonDetail!.DetailId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.ActualYield, (double?)null)
                            .SetProperty(d => d.Status, newStatus)
                            .SetProperty(d => d.UpdatedAt, DateHelper.NowVietnamTime()));
                    
                    // Auto update CropSeason status
                    await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(progress.CropSeasonDetail.CropSeasonId);
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
                    asNoTracking: true // Sử dụng asNoTracking để tránh tracking conflict
                );

                if (detail == null) return;

                // Parse current status
                if (!Enum.TryParse<CropDetailStatus>(detail.Status, out var currentStatus))
                {
                    currentStatus = CropDetailStatus.Planned;
                }

                bool statusChanged = false;
                CropDetailStatus newStatus = currentStatus;

                // Determine new status based on current state
                if (currentStatus == CropDetailStatus.Planned)
                {
                    // First progress created -> move to InProgress
                    newStatus = CropDetailStatus.InProgress;
                    statusChanged = true;
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
                        newStatus = CropDetailStatus.Completed;
                        statusChanged = true;
                    }
                }

                // Update status if changed using ExecuteUpdateAsync to avoid tracking conflicts
                if (statusChanged)
                {
                    await _unitOfWork.CropSeasonDetailRepository.GetAllQueryable()
                        .Where(d => d.DetailId == cropSeasonDetailId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.Status, newStatus.ToString())
                            .SetProperty(d => d.UpdatedAt, DateHelper.NowVietnamTime()));

                    // Auto update CropSeason status
                    await _cropSeasonService.AutoUpdateCropSeasonStatusAsync(detail.CropSeasonId);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid affecting progress creation
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

        private CropStage? GetNextValidStage(List<CropProgress> existingProgress)
        {
            try
            {
                // Get all stages ordered by OrderIndex
                var allStages = _unitOfWork.CropStageRepository.GetAllAsync().Result
                    .OrderBy(s => s.OrderIndex)
                    .ToList();

                if (!allStages.Any()) return null;

                // Get completed stage codes
                var completedStageCodes = existingProgress
                    .Where(p => !p.IsDeleted)
                    .Select(p => p.Stage?.StageCode?.ToUpper())
                    .Where(code => !string.IsNullOrEmpty(code))
                    .ToHashSet();

                // Find the next stage that hasn't been completed
                var nextStage = allStages.FirstOrDefault(stage => 
                    !completedStageCodes.Contains(stage.StageCode?.ToUpper()));

                return nextStage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting next valid stage: {ex.Message}");
                return null;
            }
        }
    }
}

