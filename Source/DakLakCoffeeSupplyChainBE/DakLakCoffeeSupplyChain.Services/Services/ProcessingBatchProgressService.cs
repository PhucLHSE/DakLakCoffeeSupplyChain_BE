using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingBatchProgressService : IProcessingBatchProgressService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingBatchProgressService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin, bool isManager)
        {
            // 1. Lấy toàn bộ progress (bao gồm Stage nếu cần hiển thị)
            var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                predicate: x => !x.IsDeleted,
                include: q => q
                    .Include(x => x.Stage)
                    .Include(x => x.UpdatedByNavigation).ThenInclude(u => u.User),
                asNoTracking: true
            );

            if (progresses == null || !progresses.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<ProcessingBatchProgressViewAllDto>());
            }

            // 2. Lấy danh sách batch liên quan (chỉ BatchId và các field cần để lọc)
            var batchIds = progresses.Select(p => p.BatchId).Distinct().ToList();

            var batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                predicate: x => batchIds.Contains(x.BatchId) && !x.IsDeleted,
                include: q => q
                    .Include(b => b.CropSeason).ThenInclude(cs => cs.Commitment)
                    .Include(b => b.Farmer),
                asNoTracking: true
            );

            var batchDict = batches.ToDictionary(b => b.BatchId, b => b);

            // 3. Lọc theo vai trò
            if (!isAdmin)
            {
                if (isManager)
                {

                    var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);
                    if (manager == null)
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin Business Manager.");
                    }

                    var managerId = manager.ManagerId;


                    progresses = progresses
                        .Where(p => batchDict.ContainsKey(p.BatchId) &&
                                    batchDict[p.BatchId].CropSeason?.Commitment?.ApprovedBy == managerId)
                        .ToList();

                    if (!progresses.Any())
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập bất kỳ tiến trình nào.");
                    }
                }
                else
                {

                    var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
                    if (farmer == null)
                        return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");

                    progresses = progresses
                        .Where(p => batchDict.ContainsKey(p.BatchId) &&
                                    batchDict[p.BatchId].FarmerId == farmer.FarmerId)
                        .ToList();
                }
            }

            // 4. Map kết quả
            var dtoList = progresses.Select(p =>
            {
                var batch = batchDict.ContainsKey(p.BatchId) ? batchDict[p.BatchId] : null;
                return p.MapToProcessingBatchProgressViewAllDto(batch);
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
        }


        public async Task<IServiceResult> GetByIdAsync(Guid progressId)
        {
            var entity = await _unitOfWork.ProcessingBatchProgressRepository
                .GetByIdAsync(progressId);

            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình xử lý", null);

            var dto = entity.MapToProcessingBatchProgressDetailDto();

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Thành công", dto);
        }
        public async Task<IServiceResult> CreateAsync(
            Guid batchId,
            ProcessingBatchProgressCreateDto input,
            Guid userId,
            bool isAdmin,
            bool isManager)
        {
            try
            {
                // 1. Kiểm tra batch hợp lệ
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Batch không tồn tại.");
                }

                // 2. Nếu không phải Admin hoặc Manager thì phải là đúng Farmer
                if (!isAdmin && !isManager)
                {
                    var farmer = (await _unitOfWork.FarmerRepository
                        .GetAllAsync(f => f.UserId == userId && !f.IsDeleted))
                        .FirstOrDefault();

                    if (farmer == null)
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy nông hộ.");

                    if (batch.FarmerId != farmer.FarmerId)
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Bạn không có quyền tạo tiến trình cho batch này.");
                }

                // 3. Lấy danh sách công đoạn (stage) theo MethodId
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (!stages.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chưa có công đoạn nào cho phương pháp chế biến này.");

                // 4. Tìm bước tiếp theo
                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                int nextStepIndex;
                int nextStageId;

                if (!progresses.Any())
                {
                    nextStageId = stages[0].StageId;
                    nextStepIndex = stages[0].OrderIndex;
                }
                else
                {
                    var latestProgress = progresses.First();
                    var currentStageIndex = stages.FindIndex(s => s.StageId == latestProgress.StageId);

                    if (currentStageIndex == -1 || currentStageIndex >= stages.Count - 1)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không thể tạo bước tiếp theo. Công đoạn cuối cùng đã hoàn tất.");

                    var nextStage = stages[currentStageIndex + 1];
                    nextStageId = nextStage.StageId;
                    nextStepIndex = nextStage.OrderIndex;
                }

                // 5. Lấy danh sách parameters cho Stage này
                var parameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                    p => p.Progress.StageId == nextStageId && !p.IsDeleted,
                    include: q => q.Include(p => p.Progress)
                );

                // 6. Tạo tiến trình mới
                var progress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batchId,
                    StepIndex = nextStepIndex,
                    StageId = nextStageId,
                    StageDescription = "",
                    ProgressDate = input.ProgressDate,
                    OutputQuantity = input.OutputQuantity,
                    OutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                    PhotoUrl = input.PhotoUrl,
                    VideoUrl = input.VideoUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = batch.FarmerId,
                    IsDeleted = false,
                    ProcessingParameters = parameters?.Select(p => new ProcessingParameter
                    {
                        ParameterId = Guid.NewGuid(),
                        ParameterName = p.ParameterName,
                        Unit = p.Unit,
                        ParameterValue = null,
                        RecordedAt = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }).ToList()
                };

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);

                // 7. Nếu batch đang NotStarted thì chuyển sang InProgress
                if (batch.Status == ProcessingStatus.NotStarted.ToString())
                {
                    batch.Status = ProcessingStatus.InProgress.ToString();
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                }

                var result = await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Đã tạo bước tiến trình thành công.", progress.ProgressId);

            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
        public async Task UpdateMediaUrlsAsync(Guid progressId, string? photoUrl, string? videoUrl)
        {
            var progress = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(progressId);
            if (progress == null) return;

            progress.PhotoUrl = photoUrl;
            progress.VideoUrl = videoUrl;
            progress.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchProgressRepository.UpdateAsync(progress);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IServiceResult> UpdateAsync(Guid progressId, ProcessingBatchProgressUpdateDto dto)
        {
            // [Step 1] Lấy entity từ DB
            var entity = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(
                p => p.ProgressId == progressId && !p.IsDeleted
            );

            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, $"[Step 1] Không tìm thấy tiến độ với ID = {progressId}");

            // [Step 2] Kiểm tra StepIndex trùng (nếu thay đổi)
            if (dto.StepIndex != entity.StepIndex)
            {
                var isDuplicated = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    p => p.BatchId == entity.BatchId &&
                         p.StepIndex == dto.StepIndex &&
                         p.ProgressId != progressId &&
                         !p.IsDeleted
                );

                if (isDuplicated)
                    return new ServiceResult(Const.FAIL_UPDATE_CODE, $"[Step 2] StepIndex {dto.StepIndex} đã tồn tại trong Batch.");
            }

            // [Step 3] So sánh và cập nhật nếu có thay đổi
            bool isModified = false;

            if (entity.StepIndex != dto.StepIndex)
            {
                entity.StepIndex = dto.StepIndex;
                isModified = true;
            }

            if (entity.OutputQuantity != dto.OutputQuantity)
            {
                entity.OutputQuantity = dto.OutputQuantity;
                isModified = true;
            }

            if (!string.Equals(entity.OutputUnit, dto.OutputUnit, StringComparison.OrdinalIgnoreCase))
            {
                entity.OutputUnit = dto.OutputUnit;
                isModified = true;
            }

            if (entity.PhotoUrl != dto.PhotoUrl)
            {
                entity.PhotoUrl = dto.PhotoUrl;
                isModified = true;
            }

            if (entity.VideoUrl != dto.VideoUrl)
            {
                entity.VideoUrl = dto.VideoUrl;
                isModified = true;
            }

            var dtoDateOnly = DateOnly.FromDateTime(dto.ProgressDate);
            if (entity.ProgressDate != dtoDateOnly)
            {
                entity.ProgressDate = dtoDateOnly;
                isModified = true;
            }

            if (!isModified)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 4] Dữ liệu truyền vào không có gì khác biệt.");
            }

            entity.UpdatedAt = DateTime.UtcNow;

            // [Step 4] Gọi UpdateAsync và kiểm tra trả về bool
            var updated = await _unitOfWork.ProcessingBatchProgressRepository.UpdateAsync(entity);
            if (!updated)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 5] UpdateAsync() trả về false.");
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                var resultDto = entity.MapToProcessingBatchProgressDetailDto();

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "[Step 6] Cập nhật thành công.", dto);
            }
            else
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 6] Không có thay đổi nào được lưu.");
            }
        }
        public async Task<IServiceResult> SoftDeleteAsync(Guid progressId)
        {
            try
            {
                var success = await _unitOfWork.ProcessingBatchProgressRepository.SoftDeleteAsync(progressId);

                if (!success)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "[SoftDelete] Progress không tồn tại hoặc đã bị xoá."
                    );
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(
                    Const.SUCCESS_DELETE_CODE,
                    "[SoftDelete] Đã xoá mềm tiến độ sơ chế thành công."
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"[Exception] {ex.Message}"
                );
            }
        }
        public async Task<IServiceResult> HardDeleteAsync(Guid progressId)
        {
            try
            {
                var success = await _unitOfWork.ProcessingBatchProgressRepository.HardDeleteAsync(progressId);
                if (!success)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Progress hoặc đã bị xóa.");
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa vĩnh viễn Progress thành công.");
            }
            catch (DbUpdateException dbEx)
            {
                // Trả về lỗi chi tiết từ SQL
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return new ServiceResult(Const.ERROR_EXCEPTION, $"[DB Error] {innerMessage}");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"[Exception] {ex.Message}");
            }
        }
        public async Task<IServiceResult> AdvanceProgressByBatchIdAsync(
    Guid batchId,
    AdvanceProcessingBatchProgressDto input,
    Guid userId,
    bool isAdmin,
    bool isManager)
        {
            try
            {
                if (batchId == Guid.Empty)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "BatchId không hợp lệ.");

                if (isAdmin || isManager)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Chỉ nông hộ mới được phép cập nhật tiến trình.");

                // Lấy Farmer từ userId
                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId == userId && !f.IsDeleted)).FirstOrDefault();
                if (farmer == null)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy nông hộ.");

                // Lấy Batch
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null || batch.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Batch không tồn tại.");

                if (batch.FarmerId != farmer.FarmerId)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không có quyền cập nhật batch này.");

                // Lấy danh sách các stage theo method → dùng để mapping StepIndex → StageId
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count == 0)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có công đoạn nào cho phương pháp này.");

                // Lấy progress cuối cùng
                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                ProcessingBatchProgress? latestProgress = progresses.FirstOrDefault();

                int nextStepIndex;
                ProcessingStage? nextStage;

                if (latestProgress == null)
                {
                    // Chưa có bước nào → bắt đầu từ StepIndex 1 và Stage đầu tiên
                    nextStepIndex = 1;
                    nextStage = stages.FirstOrDefault();
                }
                else
                {
                    // Đã có bước → tìm stage hiện tại
                    int currentStageIdx = stages.FindIndex(s => s.StageId == latestProgress.StageId);
                    if (currentStageIdx == -1)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy công đoạn hiện tại.");

                    if (currentStageIdx >= stages.Count - 1)
                    {
                        batch.Status = ProcessingStatus.Completed.ToString();
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                        await _unitOfWork.SaveChangesAsync();

                        return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Batch đã hoàn tất toàn bộ tiến trình.");
                    }

                    nextStepIndex = latestProgress.StepIndex + 1;
                    nextStage = stages[currentStageIdx + 1];
                }

                if (nextStage == null)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy công đoạn kế tiếp.");

                var newProgress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batch.BatchId,
                    StepIndex = nextStepIndex,
                    StageId = nextStage.StageId,
                    StageDescription = nextStage.Description ?? "",
                    ProgressDate = DateOnly.FromDateTime(input.ProgressDate),
                    OutputQuantity = input.OutputQuantity,
                    OutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                    PhotoUrl = input.PhotoUrl,
                    VideoUrl = input.VideoUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = farmer.FarmerId,
                    IsDeleted = false
                };

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(newProgress);

                // Chuyển trạng thái batch nếu đang là NotStarted
                if (batch.Status == ProcessingStatus.NotStarted.ToString())
                {
                    batch.Status = ProcessingStatus.InProgress.ToString();
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                }

                var saveResult = await _unitOfWork.SaveChangesAsync();

                return saveResult > 0
                    ? new ServiceResult(Const.SUCCESS_CREATE_CODE, "Đã tạo bước tiến trình kế tiếp.")
                    : new ServiceResult(Const.FAIL_CREATE_CODE, "Không thể tạo bước kế tiếp.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


    }
}
