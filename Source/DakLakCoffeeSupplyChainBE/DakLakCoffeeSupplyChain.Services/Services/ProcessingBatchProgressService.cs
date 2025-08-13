using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
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
            private readonly ICodeGenerator _codeGenerator;

            public ProcessingBatchProgressService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
                _codeGenerator = codeGenerator;
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

            // Lấy toàn bộ media từ bảng MediaFile
            var mediaFiles = await _unitOfWork.MediaFileRepository.GetAllAsync(
                m => !m.IsDeleted &&
                     m.RelatedEntity == "ProcessingProgress" &&
                     m.RelatedId == progressId,
                orderBy: q => q.OrderByDescending(m => m.UploadedAt)
            );

            var mediaDtos = mediaFiles.Select(m => new MediaFileResponse
            {
                MediaId = m.MediaId,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType,
                Caption = m.Caption,
                UploadedAt = m.UploadedAt
            }).ToList();

            var dto = entity.MapToProcessingBatchProgressDetailDto();
            
            // Chỉ lấy PhotoUrl và VideoUrl từ MediaFile, không dùng từ ProcessingBatchProgress
            var photoFiles = mediaFiles.Where(m => m.MediaType == "image").ToList();
            var videoFiles = mediaFiles.Where(m => m.MediaType == "video").ToList();
            
            // Ghi đè PhotoUrl và VideoUrl từ MediaFile
            dto.PhotoUrl = photoFiles.Any() ? photoFiles.First().MediaUrl : null;
            dto.VideoUrl = videoFiles.Any() ? videoFiles.First().MediaUrl : null;
            
            // Thêm media vào DTO
            dto.MediaFiles = mediaDtos;

            return new ServiceResult(Const.SUCCESS_READ_CODE, "Thành công", dto);
        }

        public async Task<IServiceResult> GetAllByBatchIdAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                // 1. Kiểm tra batch tồn tại
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: b => b.BatchId == batchId && !b.IsDeleted,
                    include: q => q
                        .Include(b => b.CropSeason).ThenInclude(cs => cs.Commitment)
                        .Include(b => b.Farmer),
                    asNoTracking: true
                );

                if (batch == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô sơ chế.");
                }

                // 2. Kiểm tra quyền truy cập
                if (!isAdmin)
                {
                    if (isManager)
                    {
                        var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                            m => m.UserId == userId && !m.IsDeleted,
                            asNoTracking: true
                        );

                        if (manager == null)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin Business Manager.");
                        }

                        if (batch.CropSeason?.Commitment?.ApprovedBy != manager.ManagerId)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập lô sơ chế này.");
                        }
                    }
                    else
                    {
                        var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                            f => f.UserId == userId && !f.IsDeleted,
                            asNoTracking: true
                        );

                        if (farmer == null)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");
                        }

                        if (batch.FarmerId != farmer.FarmerId)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập lô sơ chế này.");
                        }
                    }
                }

                // 3. Lấy tất cả progress của batch
                var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    predicate: p => p.BatchId == batchId && !p.IsDeleted,
                    include: q => q
                        .Include(p => p.Stage)
                        .Include(p => p.UpdatedByNavigation).ThenInclude(u => u.User)
                        .Include(p => p.ProcessingParameters.Where(pp => !pp.IsDeleted)),
                    orderBy: q => q.OrderBy(p => p.StepIndex),
                    asNoTracking: true
                );

                // Debug: Kiểm tra parameters
                foreach (var progress in progresses)
                {
                    Console.WriteLine($"Progress {progress.ProgressId}: {progress.ProcessingParameters?.Count ?? 0} parameters");
                    if (progress.ProcessingParameters?.Any() == true)
                    {
                        foreach (var param in progress.ProcessingParameters)
                        {
                            Console.WriteLine($"  - {param.ParameterName}: {param.ParameterValue} {param.Unit}");
                        }
                    }
                }

                if (!progresses.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chưa có tiến trình nào cho lô sơ chế này.", new List<ProcessingBatchProgressViewAllDto>());
                }

                // 4. Lấy media files cho từng progress
                var dtoList = new List<ProcessingBatchProgressViewAllDto>();
                
                foreach (var progress in progresses)
                {
                    var dto = progress.MapToProcessingBatchProgressViewAllDto(batch);
                    
                    // Lấy media files từ MediaFile table
                    var mediaFiles = await _unitOfWork.MediaFileRepository.GetAllAsync(
                        m => !m.IsDeleted &&
                             m.RelatedEntity == "ProcessingProgress" &&
                             m.RelatedId == progress.ProgressId,
                        orderBy: q => q.OrderByDescending(m => m.UploadedAt)
                    );
                    
                    // Chỉ lấy PhotoUrl và VideoUrl từ MediaFile, không dùng từ ProcessingBatchProgress
                    var photoFiles = mediaFiles.Where(m => m.MediaType == "image").ToList();
                    var videoFiles = mediaFiles.Where(m => m.MediaType == "video").ToList();
                    
                    // Ghi đè PhotoUrl và VideoUrl từ MediaFile
                    dto.PhotoUrl = photoFiles.Any() ? photoFiles.First().MediaUrl : null;
                    dto.VideoUrl = videoFiles.Any() ? videoFiles.First().MediaUrl : null;
                    
                    dtoList.Add(dto);
                }

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtoList);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
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

                // 3. Kiểm tra khối lượng còn lại của batch
                var existingProgresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex))).ToList();

                // Tính tổng khối lượng đã chế biến
                var totalProcessedQuantity = existingProgresses
                    .Where(p => p.OutputQuantity.HasValue)
                    .Sum(p => p.OutputQuantity.Value);

                // Khối lượng còn lại = InputQuantity - totalProcessedQuantity
                var remainingQuantity = batch.InputQuantity - totalProcessedQuantity;

                if (remainingQuantity <= 0)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, 
                        $"Không thể tạo tiến độ mới. Khối lượng cà phê đã được chế biến hết. " +
                        $"Tổng đầu vào: {batch.InputQuantity} {batch.InputUnit}, " +
                        $"Đã chế biến: {totalProcessedQuantity} {batch.InputUnit}, " +
                        $"Còn lại: {remainingQuantity} {batch.InputUnit}");
                }

                // Kiểm tra nếu có OutputQuantity trong input thì phải <= remainingQuantity
                if (input.OutputQuantity.HasValue && input.OutputQuantity.Value > remainingQuantity)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, 
                        $"Khối lượng đầu ra ({input.OutputQuantity.Value} {input.OutputUnit ?? batch.InputUnit}) " +
                        $"không thể lớn hơn khối lượng còn lại ({remainingQuantity} {batch.InputUnit})");
                }

                // 4. Lấy danh sách công đoạn (stage) theo MethodId
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (!stages.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chưa có công đoạn nào cho phương pháp chế biến này.");

                // 5. Xác định bước tiếp theo (sử dụng existingProgresses đã lấy ở trên)

                // 6. Xác định bước tiếp theo
                int nextStepIndex;
                int nextStageId;
                bool isLastStep = false;

                if (!existingProgresses.Any())
                {
                    // Bước đầu tiên
                    nextStageId = stages[0].StageId;
                    nextStepIndex = stages[0].OrderIndex;
                    // Kiểm tra nếu chỉ có 1 stage thì đó là bước cuối
                    isLastStep = (stages.Count == 1);
                }
                else
                {
                    // Tìm bước tiếp theo
                    var latestProgress = existingProgresses.Last();
                    var currentStageIndex = stages.FindIndex(s => s.StageId == latestProgress.StageId);

                    if (currentStageIndex == -1 || currentStageIndex >= stages.Count - 1)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không thể tạo bước tiếp theo. Công đoạn cuối cùng đã hoàn tất.");

                    var nextStage = stages[currentStageIndex + 1];
                    nextStageId = nextStage.StageId;
                    nextStepIndex = nextStage.OrderIndex;
                    
                    // Kiểm tra có phải bước cuối không - ĐẾM SỐ LƯỢNG STAGES
                    isLastStep = (currentStageIndex + 1 == stages.Count - 1);
                }

                // 7. Lấy danh sách parameters cho Stage này
                var stageParameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                    p => p.Progress.StageId == nextStageId && !p.IsDeleted,
                    include: q => q.Include(p => p.Progress)
                );

                // 8. Tạo tiến trình mới
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
                    ProcessingParameters = new List<ProcessingParameter>()
                };

                // Lưu progress trước để có ProgressId
                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);
                await _unitOfWork.SaveChangesAsync();

                // 9. Tạo parameters - luôn tạo (từ input hoặc mặc định)
                Console.WriteLine($"DEBUG: Input parameters count: {input.Parameters?.Count ?? 0}");
                Console.WriteLine($"DEBUG: Stage parameters count: {stageParameters?.Count ?? 0}");
                
                var parametersToCreate = new List<ProcessingParameter>();
                
                if (input.Parameters?.Any() == true)
                {
                    Console.WriteLine($"DEBUG: Creating {input.Parameters.Count} parameters from input for progress {progress.ProgressId}");
                    
                    // Tạo parameters từ input
                    parametersToCreate = input.Parameters.Select(p => new ProcessingParameter
                    {
                        ParameterId = Guid.NewGuid(),
                        ProgressId = progress.ProgressId,
                        ParameterName = p.ParameterName,
                        ParameterValue = p.ParameterValue,
                        Unit = p.Unit,
                        RecordedAt = p.RecordedAt ?? DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }).ToList();
                }
                else if (stageParameters?.Any() == true)
                {
                    Console.WriteLine($"DEBUG: Creating {stageParameters.Count} default parameters for progress {progress.ProgressId}");
                    
                    // Tạo parameters mặc định từ stage
                    parametersToCreate = stageParameters.Select(p => new ProcessingParameter
                    {
                        ParameterId = Guid.NewGuid(),
                        ProgressId = progress.ProgressId,
                        ParameterName = p.ParameterName,
                        Unit = p.Unit,
                        ParameterValue = null, // Giá trị mặc định là null
                        RecordedAt = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }).ToList();
                }
                
                // Luôn tạo parameters nếu có (từ input hoặc mặc định)
                if (parametersToCreate.Any())
                {
                    Console.WriteLine($"DEBUG: Creating {parametersToCreate.Count} parameters total");
                    
                    foreach (var param in parametersToCreate)
                    {
                        Console.WriteLine($"DEBUG: Creating parameter: {param.ParameterName} = {param.ParameterValue} {param.Unit}");
                        await _unitOfWork.ProcessingParameterRepository.CreateAsync(param);
                    }
                    
                    // Lưu parameters ngay lập tức
                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG: Parameters saved successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG: No parameters to create (no input and no stage parameters)");
                }

                // 10. Xử lý workflow theo bước
                if (!existingProgresses.Any())
                {
                    // Bước đầu tiên: Chuyển từ NotStarted sang InProgress
                    if (batch.Status == ProcessingStatus.NotStarted.ToString())
                    {
                        batch.Status = ProcessingStatus.InProgress.ToString();
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }
                }
                else if (isLastStep)
                {
                    // Bước cuối cùng: Chuyển sang AwaitingEvaluation và tạo evaluation
                    batch.Status = "AwaitingEvaluation";
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                    // Tạo evaluation cho expert
                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        BatchId = batchId,
                        EvaluatedBy = null, // Sẽ được expert cập nhật khi đánh giá
                        EvaluatedAt = null,
                        EvaluationResult = null,
                        Comments = $"Tự động tạo evaluation khi hoàn thành bước cuối cùng: {stages.Last().StageName}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                }

                var result = await _unitOfWork.SaveChangesAsync();

                var responseMessage = isLastStep 
                    ? "Đã tạo bước cuối cùng và chuyển sang chờ đánh giá từ chuyên gia." 
                    : "Đã tạo bước tiến trình thành công.";

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, responseMessage, progress.ProgressId);

            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }


        public async Task<IServiceResult> UpdateAsync(Guid progressId, ProcessingBatchProgressUpdateDto dto)
        {
            // [Step 1] Lấy entity từ DB
            var entity = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(
                p => p.ProgressId == progressId && !p.IsDeleted
            );

            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, $"[Step 1] Không tìm thấy tiến độ với ID = {progressId}");

            // [Step 1.5] Kiểm tra batch status - chỉ cho phép update khi batch đang InProgress
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(entity.BatchId);
            if (batch == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "[Step 1.5] Không tìm thấy batch tương ứng.");
            
            if (batch.Status != "InProgress")
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "[Step 1.5] Chỉ có thể cập nhật tiến độ khi batch đang trong trạng thái 'Đang thực hiện'.");

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

            // Không cập nhật PhotoUrl và VideoUrl từ dto nữa
            // Hệ thống sẽ tự động lấy từ MediaFile table

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

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "[Step 6] Cập nhật thành công.", resultDto);
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
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Starting advance for batchId: {batchId}, userId: {userId}");
                
                if (batchId == Guid.Empty)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "BatchId không hợp lệ.");

                // if (isAdmin || isManager)
                //     return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Chỉ nông hộ mới được phép cập nhật tiến trình.");

                // Lấy Farmer từ userId
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Looking for farmer with userId: {userId}");
                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId == userId && !f.IsDeleted)).FirstOrDefault();
                if (farmer == null)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Farmer not found for userId: {userId}");
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy nông hộ.");
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found farmer: {farmer.FarmerId}");

                // Lấy Batch
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting batch: {batchId}");
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Batch not found or deleted: {batchId}");
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Batch không tồn tại.");
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found batch: {batch.BatchId}, status: {batch.Status}, farmerId: {batch.FarmerId}");

                if (batch.FarmerId != farmer.FarmerId)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Permission denied - batch farmer: {batch.FarmerId}, current farmer: {farmer.FarmerId}");
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không có quyền cập nhật batch này.");
                }

                // Lấy danh sách các stage theo method → dùng để mapping StepIndex → StageId
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting stages for methodId: {batch.MethodId}");
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count == 0)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: No stages found for methodId: {batch.MethodId}");
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có công đoạn nào cho phương pháp này.");
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found {stages.Count} stages");

                // Lấy progress cuối cùng
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting latest progress for batchId: {batchId}");
                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                ProcessingBatchProgress? latestProgress = progresses.FirstOrDefault();
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found {progresses.Count} progresses, latest: {(latestProgress != null ? $"stepIndex: {latestProgress.StepIndex}, stageId: {latestProgress.StageId}" : "none")}");

                int nextStepIndex;
                ProcessingStage? nextStage;

                bool isLastStep = false;

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Calculating next step...");
                if (latestProgress == null)
                {
                    // Chưa có bước nào → bắt đầu từ StepIndex 1 và Stage đầu tiên
                    nextStepIndex = 1;
                    nextStage = stages.FirstOrDefault();
                    // Kiểm tra nếu chỉ có 1 stage thì đó là bước cuối
                    isLastStep = (stages.Count == 1);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: No previous progress, starting with stepIndex: {nextStepIndex}, stageId: {nextStage?.StageId}, isLastStep: {isLastStep}");
                }
                else
                {
                    // Đã có bước → tìm stage hiện tại
                    int currentStageIdx = stages.FindIndex(s => s.StageId == latestProgress.StageId);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Current stage index: {currentStageIdx}, total stages: {stages.Count}");
                    
                    if (currentStageIdx == -1)
                    {
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Current stage not found in stages list");
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy công đoạn hiện tại.");
                    }

                    if (currentStageIdx >= stages.Count - 1)
                    {
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: All stages completed, creating evaluation");
                        // Đã hoàn thành tất cả stages - tạo evaluation và chuyển sang AwaitingEvaluation
                        batch.Status = "AwaitingEvaluation";
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                        // Tạo evaluation cho expert
                        var evaluation = new ProcessingBatchEvaluation
                        {
                            EvaluationId = Guid.NewGuid(),
                            BatchId = batchId,
                            EvaluatedBy = null,
                            EvaluatedAt = null,
                            EvaluationResult = null,
                            Comments = $"Tự động tạo evaluation khi hoàn thành bước cuối cùng: {stages.Last().StageName}",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                        await _unitOfWork.SaveChangesAsync();

                        return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Đã hoàn thành tất cả tiến trình và chuyển sang chờ đánh giá từ chuyên gia.");
                    }

                    nextStepIndex = latestProgress.StepIndex + 1;
                    nextStage = stages[currentStageIdx + 1];
                    // Kiểm tra có phải bước cuối không - ĐẾM SỐ LƯỢNG STAGES
                    isLastStep = (currentStageIdx + 1 == stages.Count - 1);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Next stepIndex: {nextStepIndex}, nextStageId: {nextStage?.StageId}, isLastStep: {isLastStep}");
                }

                if (nextStage == null)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy công đoạn kế tiếp.");

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Creating new progress - stepIndex: {nextStepIndex}, stageId: {nextStage.StageId}");
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
                    IsDeleted = false,
                    ProcessingParameters = new List<ProcessingParameter>()
                };

                // Lưu progress trước để có ProgressId
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Saving progress to database...");
                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(newProgress);
                await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Progress saved successfully with ID: {newProgress.ProgressId}");

                // Tạo parameters mặc định cho stage này (nếu có)
                // Lấy parameters từ ProcessingParameter table dựa trên StageId
                var stageParameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                    p => p.Progress.StageId == nextStage.StageId && !p.IsDeleted,
                    include: q => q.Include(p => p.Progress)
                );

                // Tạo parameters - luôn tạo (từ input hoặc mặc định)
                Console.WriteLine($"DEBUG ADVANCE: Input parameters count: {input.Parameters?.Count ?? 0}");
                Console.WriteLine($"DEBUG ADVANCE: Stage parameters count: {stageParameters?.Count ?? 0}");
                
                var parametersToCreate = new List<ProcessingParameter>();
                
                if (input.Parameters?.Any() == true)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Creating {input.Parameters.Count} parameters from input for progress {newProgress.ProgressId}");
                    
                    // Tạo parameters từ input
                    parametersToCreate = input.Parameters.Select(p => new ProcessingParameter
                    {
                        ParameterId = Guid.NewGuid(),
                        ProgressId = newProgress.ProgressId,
                        ParameterName = p.ParameterName,
                        ParameterValue = p.ParameterValue,
                        Unit = p.Unit,
                        RecordedAt = p.RecordedAt ?? DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }).ToList();
                }
                else if (stageParameters?.Any() == true)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Creating {stageParameters.Count} default parameters for progress {newProgress.ProgressId}");
                    
                    // Tạo parameters mặc định từ stage
                    parametersToCreate = stageParameters.Select(p => new ProcessingParameter
                    {
                        ParameterId = Guid.NewGuid(),
                        ProgressId = newProgress.ProgressId,
                        ParameterName = p.ParameterName,
                        Unit = p.Unit,
                        ParameterValue = null, // Giá trị mặc định là null
                        RecordedAt = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    }).ToList();
                }
                
                // Luôn tạo parameters nếu có (từ input hoặc mặc định)
                if (parametersToCreate.Any())
                {
                    Console.WriteLine($"DEBUG ADVANCE: Creating {parametersToCreate.Count} parameters total");
                    
                    foreach (var param in parametersToCreate)
                    {
                        Console.WriteLine($"DEBUG ADVANCE: Creating parameter: {param.ParameterName} = {param.ParameterValue} {param.Unit}");
                        await _unitOfWork.ProcessingParameterRepository.CreateAsync(param);
                    }
                    
                    // Lưu parameters ngay lập tức
                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG ADVANCE: Parameters saved successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG ADVANCE: No parameters to create (no input and no stage parameters)");
                }

                // Xử lý workflow theo bước
                bool hasChanges = false;
                
                if (latestProgress == null)
                {
                    // Bước đầu tiên: Chuyển từ NotStarted sang InProgress
                    if (batch.Status == ProcessingStatus.NotStarted.ToString())
                    {
                        batch.Status = ProcessingStatus.InProgress.ToString();
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                        hasChanges = true;
                    }
                }
                else if (isLastStep)
                {
                    // Bước cuối cùng: Chuyển sang AwaitingEvaluation và tạo evaluation
                    batch.Status = "AwaitingEvaluation";
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                    // Tạo evaluation cho expert
                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        BatchId = batchId,
                        EvaluatedBy = null,
                        EvaluatedAt = null,
                        EvaluationResult = null,
                        Comments = $"Tự động tạo evaluation khi hoàn thành bước cuối cùng: {stages.Last().StageName}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                    hasChanges = true;
                }

                var responseMessage = isLastStep 
                    ? "Đã tạo bước cuối cùng và chuyển sang chờ đánh giá từ chuyên gia." 
                    : "Đã tạo bước tiến trình kế tiếp.";

                Console.WriteLine($"DEBUG ADVANCE: Response message: {responseMessage}");
                Console.WriteLine($"DEBUG ADVANCE: Is last step: {isLastStep}");
                Console.WriteLine($"DEBUG ADVANCE: Has changes: {hasChanges}");

                // Chỉ save changes nếu có thay đổi
                if (hasChanges)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Final save changes...");
                    var saveResult = await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG ADVANCE: Save result: {saveResult}");
                    
                    return saveResult > 0
                        ? new ServiceResult(Const.SUCCESS_CREATE_CODE, responseMessage)
                        : new ServiceResult(Const.FAIL_CREATE_CODE, "Không thể tạo bước kế tiếp.");
                }
                else
                {
                    Console.WriteLine($"DEBUG ADVANCE: No additional changes to save");
                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, responseMessage);
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> GetAvailableBatchesForProgressAsync(Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                List<ProcessingBatch> availableBatches;

                if (isAdmin)
                {
                    // Admin có thể xem tất cả batch có thể tạo progress
                    availableBatches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                        predicate: b => !b.IsDeleted && 
                                       (b.Status == ProcessingStatus.NotStarted.ToString() || 
                                        b.Status == ProcessingStatus.InProgress.ToString()),
                        include: q => q
                            .Include(b => b.Method)
                            .Include(b => b.CropSeason)
                            .Include(b => b.CoffeeType)
                            .Include(b => b.Farmer).ThenInclude(f => f.User)
                            .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted)),
                        orderBy: q => q.OrderByDescending(b => b.CreatedAt),
                        asNoTracking: true
                    );
                }
                else if (isManager)
                {
                    // Manager chỉ xem batch của nông dân được quản lý
                    var manager = await _unitOfWork.BusinessManagerRepository
                        .GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);

                    if (manager == null)
                        return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy Business Manager tương ứng.");

                    var managerId = manager.ManagerId;

                    availableBatches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                        predicate: b => !b.IsDeleted && 
                                       (b.Status == ProcessingStatus.NotStarted.ToString() || 
                                        b.Status == ProcessingStatus.InProgress.ToString()) &&
                                       b.CropSeason != null &&
                                       b.CropSeason.Commitment != null &&
                                       b.CropSeason.Commitment.ApprovedBy == managerId,
                        include: q => q
                            .Include(b => b.Method)
                            .Include(b => b.CropSeason)
                            .Include(b => b.CoffeeType)
                            .Include(b => b.Farmer).ThenInclude(f => f.User)
                            .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted)),
                        orderBy: q => q.OrderByDescending(b => b.CreatedAt),
                        asNoTracking: true
                    );
                }
                else
                {
                    // Farmer chỉ xem batch của mình
                    var farmer = await _unitOfWork.FarmerRepository
                        .GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);

                    if (farmer == null)
                        return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");

                    availableBatches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                        predicate: b => !b.IsDeleted && 
                                       b.FarmerId == farmer.FarmerId &&
                                       (b.Status == ProcessingStatus.NotStarted.ToString() || 
                                        b.Status == ProcessingStatus.InProgress.ToString()),
                        include: q => q
                            .Include(b => b.Method)
                            .Include(b => b.CropSeason)
                            .Include(b => b.CoffeeType)
                            .Include(b => b.Farmer).ThenInclude(f => f.User)
                            .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted)),
                        orderBy: q => q.OrderByDescending(b => b.CreatedAt),
                        asNoTracking: true
                    );
                }

                if (!availableBatches.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có lô chế biến nào có thể tạo tiến độ.", new List<object>());
                }

                // Tính toán khối lượng còn lại cho mỗi batch
                var result = new List<AvailableBatchForProgressDto>();
                foreach (var batch in availableBatches)
                {
                    // Tính tổng khối lượng đã chế biến
                    var totalProcessedQuantity = batch.ProcessingBatchProgresses
                        .Where(p => p.OutputQuantity.HasValue)
                        .Sum(p => p.OutputQuantity.Value);

                    // Khối lượng còn lại = InputQuantity - totalProcessedQuantity
                    var remainingQuantity = batch.InputQuantity - totalProcessedQuantity;

                    // Chỉ trả về batch có khối lượng còn lại > 0
                    if (remainingQuantity > 0)
                    {
                        result.Add(new AvailableBatchForProgressDto
                        {
                            BatchId = batch.BatchId,
                            BatchCode = batch.BatchCode,
                            SystemBatchCode = batch.SystemBatchCode,
                            Status = batch.Status,
                            CreatedAt = batch.CreatedAt ?? DateTime.MinValue,
                            
                            // Thông tin liên kết
                            CoffeeTypeId = batch.CoffeeTypeId,
                            CoffeeTypeName = batch.CoffeeType?.TypeName ?? "N/A",
                            CropSeasonId = batch.CropSeasonId,
                            CropSeasonName = batch.CropSeason?.SeasonName ?? "N/A",
                            MethodId = batch.MethodId,
                            MethodName = batch.Method?.Name ?? "N/A",
                            FarmerId = batch.FarmerId,
                            FarmerName = batch.Farmer?.User?.Name ?? "N/A",
                            
                            // Thông tin khối lượng
                            TotalInputQuantity = batch.InputQuantity,
                            TotalProcessedQuantity = totalProcessedQuantity,
                            RemainingQuantity = remainingQuantity,
                            InputUnit = batch.InputUnit,
                            
                            // Thông tin tiến độ
                            TotalProgresses = batch.ProcessingBatchProgresses.Count,
                            LastProgressDate = batch.ProcessingBatchProgresses
                                .OrderByDescending(p => p.ProgressDate)
                                .FirstOrDefault()?.ProgressDate
                        });
                    }
                }

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách batch có thể tạo tiến độ thành công.", result);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi lấy danh sách batch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> AdvanceProgressAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                // Kiểm tra quyền truy cập
                if (!isAdmin)
                {
                    if (isManager)
                    {
                        var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(m => m.UserId == userId && !m.IsDeleted);
                        if (manager == null)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin Business Manager.");
                        }

                        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(b => b.BatchId == batchId && !b.IsDeleted);
                        if (batch == null)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy lô chế biến.");
                        }

                        var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(c => c.CommitmentId == batch.CropSeason.CommitmentId && !c.IsDeleted);
                        if (commitment?.ApprovedBy != manager.ManagerId)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập lô chế biến này.");
                        }
                    }
                    else
                    {
                        var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
                        if (farmer == null)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin nông hộ.");
                        }

                        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(b => b.BatchId == batchId && b.FarmerId == farmer.FarmerId && !b.IsDeleted);
                        if (batch == null)
                        {
                            return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy lô chế biến hoặc bạn không có quyền truy cập.");
                        }
                    }
                }

                // Lấy batch và thông tin liên quan
                var processingBatch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: b => b.BatchId == batchId && !b.IsDeleted,
                    include: q => q
                        .Include(b => b.Method)
                        .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted).OrderBy(p => p.StepIndex))
                        .Include(b => b.ProcessingBatchProgresses).ThenInclude(p => p.Stage),
                    asNoTracking: false
                );

                if (processingBatch == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy lô chế biến.");
                }

                // Kiểm tra trạng thái batch
                if (processingBatch.Status != ProcessingStatus.InProgress.ToString() && 
                    processingBatch.Status != ProcessingStatus.NotStarted.ToString())
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Lô chế biến không ở trạng thái có thể tiến hành.");
                }

                // Lấy bước tiếp theo
                var currentStepIndex = processingBatch.ProcessingBatchProgresses.Any() 
                    ? processingBatch.ProcessingBatchProgresses.Max(p => p.StepIndex) 
                    : 0;

                var nextStepIndex = currentStepIndex + 1;

                // Lấy thông tin stage cho bước tiếp theo
                var nextStage = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(
                    predicate: s => s.MethodId == processingBatch.MethodId && s.OrderIndex == nextStepIndex && !s.IsDeleted
                );

                if (nextStage == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin bước tiếp theo.");
                }

                // Tạo progress mới cho bước tiếp theo
                var newProgress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batchId,
                    StepIndex = nextStepIndex,
                    StageId = nextStage.StageId,
                    StageDescription = nextStage.Description,
                    ProgressDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    UpdatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(newProgress);

                // Cập nhật trạng thái batch
                if (processingBatch.Status == ProcessingStatus.NotStarted.ToString())
                {
                    processingBatch.Status = ProcessingStatus.InProgress.ToString();
                    processingBatch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(processingBatch);
                }

                // Kiểm tra xem có phải bước cuối cùng không
                var totalStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    predicate: s => s.MethodId == processingBatch.MethodId && !s.IsDeleted
                );
                
                if (nextStepIndex >= totalStages.Count())
                {
                    // Tạo evaluation tự động
                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        EvaluationCode = await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year),
                        BatchId = batchId,
                        EvaluationResult = "Temporary",
                        Comments = "Đánh giá tự động sau khi hoàn thành tất cả các bước.",
                        EvaluatedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);

                    // Cập nhật trạng thái batch thành AwaitingEvaluation
                    processingBatch.Status = ProcessingStatus.AwaitingEvaluation.ToString();
                    processingBatch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(processingBatch);
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tiến hành bước tiếp theo thành công.", newProgress);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi tiến hành bước tiếp theo: {ex.Message}");
            }
        }
    }
}
