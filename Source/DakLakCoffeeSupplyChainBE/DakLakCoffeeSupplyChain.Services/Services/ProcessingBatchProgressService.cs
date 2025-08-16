using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
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
                Console.WriteLine($"DEBUG CREATE: Starting create progress for batch: {batchId}");
                Console.WriteLine($"DEBUG CREATE: User: {userId}, isAdmin: {isAdmin}, isManager: {isManager}");
                Console.WriteLine($"DEBUG CREATE: Input quantity: {input.OutputQuantity}, unit: {input.OutputUnit}");
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

                // 🔧 FIX: Bỏ logic tính remainingQuantity sai - không thể trừ InputQuantity với OutputQuantity
                // Vì InputQuantity = cà phê tươi, OutputQuantity = cà phê đã chế biến (đơn vị khác nhau)
                
                // 🔧 VALIDATION: Kiểm tra khối lượng output cơ bản
                if (input.OutputQuantity.HasValue)
                {
                    // Kiểm tra khối lượng phải > 0
                    if (input.OutputQuantity.Value <= 0)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Khối lượng đầu ra phải lớn hơn 0.");
                    }

                    // Kiểm tra khối lượng không được quá lớn (ví dụ: 100,000 kg)
                    if (input.OutputQuantity.Value > 100000)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Khối lượng đầu ra không được vượt quá 100,000.");
                    }

                    // Kiểm tra đơn vị hợp lệ
                    var validUnits = new[] { "kg", "g", "tấn", "lít", "ml", "bao", "thùng", "khác" };
                    var outputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();
                    
                    if (!validUnits.Contains(outputUnit))
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, 
                            $"Đơn vị '{input.OutputUnit}' không hợp lệ. Đơn vị hợp lệ: {string.Join(", ", validUnits)}");
                    }

                    // 🔧 VALIDATION: So sánh với progress trước đó (nếu có)
                    if (existingProgresses.Any())
                    {
                        var latestProgress = existingProgresses.Last();
                        if (latestProgress.OutputQuantity.HasValue)
                        {
                            var previousQuantity = latestProgress.OutputQuantity.Value;
                            var currentQuantity = input.OutputQuantity.Value;
                            var changePercentage = ((currentQuantity - previousQuantity) / previousQuantity) * 100;

                            Console.WriteLine($"DEBUG CREATE: Quantity comparison:");
                            Console.WriteLine($"  - Previous quantity: {previousQuantity} {latestProgress.OutputUnit}");
                            Console.WriteLine($"  - Current quantity: {currentQuantity} {input.OutputUnit ?? batch.InputUnit}");
                            Console.WriteLine($"  - Change: {changePercentage:F2}%");

                            // 🔧 CẢI THIỆN: Thêm tolerance cho khối lượng (10%)
                            const double tolerance = 0.1; // 10% tolerance
                            
                            if (currentQuantity > previousQuantity * (1 + tolerance))
                            {
                                return new ServiceResult(Const.FAIL_CREATE_CODE, 
                                    $"Khối lượng đầu ra ({currentQuantity} {input.OutputUnit ?? batch.InputUnit}) " +
                                    $"tăng quá nhiều so với lần trước ({previousQuantity} {latestProgress.OutputUnit}). " +
                                    $"Vui lòng kiểm tra lại hoặc giải thích lý do tăng khối lượng.");
                            }

                            // Nếu khối lượng giảm quá nhiều (>70%), cảnh báo
                            if (changePercentage < -70)
                            {
                                return new ServiceResult(Const.FAIL_CREATE_CODE, 
                                    $"Khối lượng đầu ra ({currentQuantity} {input.OutputUnit ?? batch.InputUnit}) giảm quá nhiều so với lần trước ({previousQuantity} {latestProgress.OutputUnit}). " +
                                    $"Vui lòng kiểm tra lại hoặc giải thích lý do hao hụt lớn.");
                            }
                        }
                    }
                }

                // 4. Lấy danh sách công đoạn (stage) theo MethodId
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (!stages.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Chưa có công đoạn nào cho phương pháp chế biến này.");

                // 5. Kiểm tra xem batch có bị Fail không và lấy thông tin stage cần retry
                var failureInfo = await GetFailureInfoForBatch(batchId);
                if (failureInfo != null)
                {
                    Console.WriteLine($"DEBUG CREATE: Found failure info - OrderIndex: {failureInfo.FailedOrderIndex}, StageName: {failureInfo.FailedStageName}");
                }
                else
                {
                    Console.WriteLine($"DEBUG CREATE: No failure info found");
                }
                
                // 6. Xác định bước tiếp theo
                int nextStepIndex;
                int nextStageId;
                bool isLastStep = false;
                bool isRetryScenario = false;

                if (!existingProgresses.Any())
                {
                    // Bước đầu tiên
                    nextStageId = stages[0].StageId;
                    nextStepIndex = 1; // Luôn bắt đầu từ 1
                    // Kiểm tra nếu chỉ có 1 stage thì đó là bước cuối
                    isLastStep = (stages.Count == 1);
                }
                else
                {
                    // Tìm bước tiếp theo
                    var latestProgress = existingProgresses.Last();
                    var currentStageIndex = stages.FindIndex(s => s.StageId == latestProgress.StageId);

                    if (currentStageIndex == -1)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy công đoạn hiện tại.");

                    // 🔧 FIX: Kiểm tra retry scenario trước
                    if (failureInfo != null && failureInfo.FailedStageId.HasValue && latestProgress.StageId == failureInfo.FailedStageId.Value)
                    {
                        // ✅ Retry stage hiện tại (stage bị fail)
                        nextStageId = latestProgress.StageId;
                        nextStepIndex = latestProgress.StepIndex + 1;
                        isLastStep = (currentStageIndex == stages.Count - 1);
                        isRetryScenario = true;
                        
                        Console.WriteLine($"DEBUG: Retry scenario - StageId: {nextStageId}, StepIndex: {nextStepIndex}, isLastStep: {isLastStep}");
                    }
                    else if (currentStageIndex >= stages.Count - 1)
                    {
                        // 🔧 FIX: Chỉ chặn nếu không phải retry scenario
                        if (!isRetryScenario) {
                            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không thể tạo bước tiếp theo. Công đoạn cuối cùng đã hoàn tất.");
                        }
                        
                        // Nếu là retry scenario, cho phép retry stage cuối
                        nextStageId = latestProgress.StageId;
                        nextStepIndex = latestProgress.StepIndex + 1;
                        isLastStep = true;
                        isRetryScenario = true;
                        
                        Console.WriteLine($"DEBUG: Retry scenario for last stage - StageId: {nextStageId}, StepIndex: {nextStepIndex}");
                    }
                    else
                    {
                        // Bước tiếp theo bình thường
                        var nextStage = stages[currentStageIndex + 1];
                        nextStageId = nextStage.StageId;
                        nextStepIndex = latestProgress.StepIndex + 1;
                        
                        // 🔧 FIX: Dùng OrderIndex để xác định stage cuối chính xác
                        var maxOrderIndex = stages.Max(s => s.OrderIndex);
                        isLastStep = (nextStage.OrderIndex >= maxOrderIndex);
                        
                        Console.WriteLine($"DEBUG: Normal next step - CurrentStageIndex: {currentStageIndex}, StagesCount: {stages.Count}, isLastStep: {isLastStep}");
                        Console.WriteLine($"DEBUG: NextStageId: {nextStageId}, NextStepIndex: {nextStepIndex}");
                        Console.WriteLine($"DEBUG: Next stage OrderIndex: {nextStage.OrderIndex}, Max OrderIndex: {maxOrderIndex}");
                    }
                }

                // 7. VALIDATION: Kiểm tra nếu input có StageId thì phải đúng Stage được phép
                if (input.StageId.HasValue && input.StageId.Value != nextStageId)
                {
                    var requestedStage = stages.FirstOrDefault(s => s.StageId == input.StageId.Value);
                    var allowedStage = stages.FirstOrDefault(s => s.StageId == nextStageId);
                    
                    return new ServiceResult(Const.FAIL_CREATE_CODE, 
                        $"Không thể tạo tiến trình cho công đoạn '{requestedStage?.StageName}'. " +
                        $"Bước tiếp theo phải là '{allowedStage?.StageName}' (thứ tự {allowedStage?.OrderIndex}).");
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
                    StageDescription = isRetryScenario ? $"Retry lần {nextStepIndex - existingProgresses.Count(p => p.StageId == nextStageId)}" : "",
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
                Console.WriteLine($"DEBUG CREATE: Processing workflow - isRetryScenario: {isRetryScenario}, isLastStep: {isLastStep}");
                
                if (!existingProgresses.Any())
                {
                    // Bước đầu tiên: Chuyển từ NotStarted sang InProgress
                    if (batch.Status == ProcessingStatus.NotStarted.ToString())
                    {
                        Console.WriteLine($"DEBUG CREATE: First step - changing status from NotStarted to InProgress");
                        batch.Status = ProcessingStatus.InProgress.ToString();
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }
                }
                else if (isRetryScenario)
                {
                    // 🔧 FIX: Xử lý retry scenario
                    Console.WriteLine($"DEBUG CREATE: Processing retry scenario for stage {nextStageId}");
                    
                    // Nếu đang ở AwaitingEvaluation, chuyển về InProgress
                    if (batch.Status == "AwaitingEvaluation")
                    {
                        Console.WriteLine($"DEBUG CREATE: Retry - changing status from AwaitingEvaluation to InProgress");
                        batch.Status = "InProgress";
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }
                    
                    // Nếu retry stage cuối và hoàn thành, chuyển sang AwaitingEvaluation
                    if (isLastStep)
                    {
                        Console.WriteLine($"DEBUG CREATE: Retry last step - changing status to AwaitingEvaluation");
                        batch.Status = "AwaitingEvaluation";
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                        
                        // Tạo evaluation mới cho expert
                        var evaluation = new ProcessingBatchEvaluation
                        {
                            EvaluationId = Guid.NewGuid(),
                            BatchId = batchId,
                            EvaluatedBy = null,
                            EvaluatedAt = null,
                            EvaluationResult = null,
                            Comments = $"Retry evaluation sau khi sửa lỗi: {stages.First(s => s.StageId == nextStageId).StageName}",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsDeleted = false
                        };

                        await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                        Console.WriteLine($"DEBUG CREATE: Created new evaluation for retry scenario");
                    }
                }
                else if (isLastStep)
                {
                    // Bước cuối cùng: Chuyển sang AwaitingEvaluation và tạo evaluation
                    Console.WriteLine($"DEBUG CREATE: Last step - changing status to AwaitingEvaluation");
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
                    Console.WriteLine($"DEBUG CREATE: Created evaluation for last step");
                }

                var result = await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"DEBUG CREATE: SaveChangesAsync result: {result}");

                var responseMessage = isRetryScenario 
                    ? $"Đã tạo bước retry cho stage {stages.First(s => s.StageId == nextStageId).StageName} thành công."
                    : isLastStep 
                        ? "Đã tạo bước cuối cùng và chuyển sang chờ đánh giá từ chuyên gia." 
                        : "Đã tạo bước tiến trình thành công.";

                Console.WriteLine($"DEBUG CREATE: Success - {responseMessage}");
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

                // 🔧 REMOVED: Logic chuyển status phức tạp - để UpdateAfterEvaluation xử lý

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

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Using stageId from input: {input.StageId}");
                
                // 🔧 FIX: Đơn giản hóa logic - luôn tự động chọn stage tiếp theo
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Auto-calculating next stage...");
                
                if (latestProgress == null)
                {
                    // Chưa có bước nào → bắt đầu từ StepIndex 1 và Stage đầu tiên
                    nextStepIndex = 1;
                    nextStage = stages.FirstOrDefault();
                    isLastStep = (stages.Count == 1);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: No previous progress, starting with stepIndex: {nextStepIndex}, stageId: {nextStage?.StageId}, isLastStep: {isLastStep}");
                }
                else
                {
                    // Đã có bước → tìm stage hiện tại và chọn stage tiếp theo
                    int currentStageIdx = stages.FindIndex(s => s.StageId == latestProgress.StageId);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Current stage index: {currentStageIdx}, total stages: {stages.Count}");
                    
                    if (currentStageIdx == -1)
                    {
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Current stage not found in stages list");
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy công đoạn hiện tại.");
                    }

                    if (currentStageIdx >= stages.Count - 1)
                    {
                        // Đã ở stage cuối cùng
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Already at last stage, cannot advance further");
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Đã hoàn thành tất cả các bước. Không thể tiến thêm nữa.");
                    }
                    else
                    {
                        // Chọn stage tiếp theo
                        nextStepIndex = latestProgress.StepIndex + 1;
                        nextStage = stages[currentStageIdx + 1];
                        
                        // 🔧 FIX: Dùng OrderIndex để xác định stage cuối chính xác
                        var maxOrderIndex = stages.Max(s => s.OrderIndex);
                        isLastStep = (nextStage.OrderIndex >= maxOrderIndex);
                        
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Next stepIndex: {nextStepIndex}, nextStageId: {nextStage?.StageId}, isLastStep: {isLastStep}");
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Next stage OrderIndex: {nextStage.OrderIndex}, Max OrderIndex: {maxOrderIndex}");
                    }
                }

                if (nextStage == null)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy công đoạn kế tiếp.");

                // 🔧 VALIDATION: Kiểm tra khối lượng output khi advance
                if (input.OutputQuantity.HasValue)
                {
                    // Kiểm tra khối lượng phải > 0
                    if (input.OutputQuantity.Value <= 0)
                    {
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Khối lượng đầu ra phải lớn hơn 0.");
                    }

                    // 🔧 VALIDATION: So sánh với progress trước đó (nếu có)
                    if (latestProgress != null && latestProgress.OutputQuantity.HasValue)
                    {
                        var previousQuantity = latestProgress.OutputQuantity.Value;
                        var currentQuantity = input.OutputQuantity.Value;
                        var changePercentage = ((currentQuantity - previousQuantity) / previousQuantity) * 100;

                        Console.WriteLine($"DEBUG ADVANCE: Quantity comparison:");
                        Console.WriteLine($"  - Previous quantity: {previousQuantity} {latestProgress.OutputUnit}");
                        Console.WriteLine($"  - Current quantity: {currentQuantity} {input.OutputUnit}");
                        Console.WriteLine($"  - Change: {changePercentage:F2}%");

                        // 🔧 CẢI THIỆN: Thêm tolerance cho khối lượng (15%)
                        const double tolerance = 0.15; // 15% tolerance
                        
                        if (currentQuantity > previousQuantity * (1 + tolerance))
                        {
                            return new ServiceResult(Const.ERROR_VALIDATION_CODE, 
                                $"Khối lượng đầu ra ({currentQuantity} {input.OutputUnit}) tăng quá nhiều so với lần trước ({previousQuantity} {latestProgress.OutputUnit}). " +
                                $"Vui lòng kiểm tra lại hoặc giải thích lý do tăng khối lượng.");
                        }

                        // Nếu khối lượng giảm quá nhiều (>70%), cảnh báo
                        if (changePercentage < -70)
                        {
                            return new ServiceResult(Const.ERROR_VALIDATION_CODE, 
                                $"Khối lượng đầu ra ({currentQuantity} {input.OutputUnit}) giảm quá nhiều so với lần trước ({previousQuantity} {latestProgress.OutputUnit}). " +
                                $"Vui lòng kiểm tra lại hoặc giải thích lý do giảm khối lượng.");
                        }
                    }

                    // 🔧 FIX: Bỏ validation sai - không thể so sánh InputQuantity với OutputQuantity
                    // Vì InputQuantity = cà phê tươi, OutputQuantity = cà phê đã chế biến
                    // Có thể có hao hụt hoặc thay đổi khối lượng trong quá trình chế biến
                }

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Creating new progress - stepIndex: {nextStepIndex}, stageId: {nextStage.StageId}");
                var newProgress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batch.BatchId,
                    StepIndex = nextStepIndex,
                    StageId = nextStage.StageId,
                    StageDescription = !string.IsNullOrWhiteSpace(input.StageDescription) ? input.StageDescription : (nextStage.Description ?? ""),
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
                    Console.WriteLine($"DEBUG ADVANCE: Updating batch status from '{batch.Status}' to 'AwaitingEvaluation'");
                    batch.Status = "AwaitingEvaluation";
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    Console.WriteLine($"DEBUG ADVANCE: Batch status updated successfully");

                    // Tạo evaluation cho expert khi hoàn thành bước cuối
                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        EvaluationCode = await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year),
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
                    Console.WriteLine($"DEBUG ADVANCE: Created new evaluation for batch with code: {evaluation.EvaluationCode}");
                }
                // 🔧 REMOVED: Logic re-update scenario phức tạp - để UpdateAfterEvaluation xử lý

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
                    try
                    {
                        var saveResult = await _unitOfWork.SaveChangesAsync();
                        Console.WriteLine($"DEBUG ADVANCE: Save result: {saveResult}");
                        Console.WriteLine($"DEBUG ADVANCE: Save result > 0: {saveResult > 0}");
                        
                        if (saveResult > 0)
                        {
                            Console.WriteLine($"DEBUG ADVANCE: Returning success response");
                            return new ServiceResult(Const.SUCCESS_CREATE_CODE, responseMessage);
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG ADVANCE: Save failed - no changes saved");
                            return new ServiceResult(Const.FAIL_CREATE_CODE, "Không thể tạo bước kế tiếp.");
                        }
                    }
                    catch (Exception saveEx)
                    {
                        Console.WriteLine($"DEBUG ADVANCE: Exception during SaveChangesAsync: {saveEx.Message}");
                        Console.WriteLine($"DEBUG ADVANCE: Exception stack trace: {saveEx.StackTrace}");
                        
                        // Log inner exception nếu có
                        if (saveEx.InnerException != null)
                        {
                            Console.WriteLine($"DEBUG ADVANCE: Inner exception: {saveEx.InnerException.Message}");
                            Console.WriteLine($"DEBUG ADVANCE: Inner exception stack trace: {saveEx.InnerException.StackTrace}");
                        }
                        
                        return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi lưu thay đổi: {saveEx.Message}");
                    }
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

        // API mới: Cập nhật progress sau khi bị đánh giá fail
        public async Task<IServiceResult> UpdateProgressAfterEvaluationAsync(
            Guid batchId,
            ProcessingBatchProgressCreateDto input,
            Guid userId,
            bool isAdmin,
            bool isManager)
        {
            try
            {
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Starting update for batchId: {batchId}, userId: {userId}");
                
                if (batchId == Guid.Empty)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "BatchId không hợp lệ.");

                // Lấy Farmer từ userId
                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId == userId && !f.IsDeleted)).FirstOrDefault();
                if (farmer == null)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy nông hộ.");
                }

                // Lấy Batch
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Batch không tồn tại.");
                }

                if (batch.FarmerId != farmer.FarmerId)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Permission denied - batch farmer: {batch.FarmerId}, current farmer: {farmer.FarmerId}");
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không có quyền cập nhật batch này.");
                }
                
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Permission granted - using farmer: {farmer.FarmerId}");
                
                // Kiểm tra farmer có tồn tại trong database không
                var farmerExists = await _unitOfWork.FarmerRepository.AnyAsync(f => f.FarmerId == farmer.FarmerId && !f.IsDeleted);
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Farmer exists in database: {farmerExists}");

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Batch status: '{batch.Status}'");
                
                // 🔧 KIỂM TRA: Chỉ cho phép cập nhật khi batch đang ở InProgress (sau khi bị fail)
                if (batch.Status != "InProgress")
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, $"Chỉ có thể cập nhật progress khi batch đang ở trạng thái InProgress (sau khi bị đánh giá fail). Trạng thái hiện tại: '{batch.Status}'");
                }

                // Lấy evaluation fail cuối cùng
                var latestEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId == batchId && !e.IsDeleted,
                    q => q.OrderByDescending(e => e.CreatedAt)
                );

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {latestEvaluation.Count} evaluations for batch {batchId}");
                
                // 🔧 FIX: Debug tất cả evaluations để xem vấn đề
                foreach (var eval in latestEvaluation)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation - ID: {eval.EvaluationId}, Result: '{eval.EvaluationResult}', CreatedAt: {eval.CreatedAt}, UpdatedAt: {eval.UpdatedAt}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Comments: {eval.Comments}");
                }
                
                var evaluation = latestEvaluation.FirstOrDefault();
                if (evaluation != null)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Selected evaluation - ID: {evaluation.EvaluationId}, Result: '{evaluation.EvaluationResult}', CreatedAt: {evaluation.CreatedAt}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation comments: {evaluation.Comments}");
                }
                
                if (evaluation == null)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không tìm thấy đánh giá nào cho batch này.");
                }
                
                if (evaluation.EvaluationResult != "Fail")
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, $"Đánh giá cuối cùng không phải là Fail. Kết quả hiện tại: '{evaluation.EvaluationResult}'");
                }

                // Parse failure info để biết stage nào bị fail
                var failureInfo = await GetFailureInfoForBatch(batchId);
                if (failureInfo == null)
                {
                    // Debug: Log comments để xem format thực tế
                    if (evaluation != null)
                    {
                        Console.WriteLine($"DEBUG: Evaluation comments: {evaluation.Comments}");
                    }
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không thể xác định stage nào cần cải thiện.");
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Failed stage - OrderIndex: {failureInfo.FailedOrderIndex}, StageName: {failureInfo.FailedStageName}");

                // Lấy danh sách stages
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count == 0)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có công đoạn nào cho phương pháp này.");
                }

                // Kiểm tra stage bị fail có tồn tại không
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Checking if stage with OrderIndex {failureInfo.FailedOrderIndex} exists in stages list");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Available stages: {string.Join(", ", stages.Select(s => $"{s.StageId}({s.StageName})[Order:{s.OrderIndex}]"))}");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Batch MethodId: {batch.MethodId}");
                
                // 🔧 FIX: Sử dụng OrderIndex để tìm stage
                var failedStage = stages.FirstOrDefault(s => s.OrderIndex == failureInfo.FailedOrderIndex);
                if (failedStage == null)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Stage with OrderIndex {failureInfo.FailedOrderIndex} not found in stages list!");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Checking all stages in database for MethodId {batch.MethodId}...");
                    
                    // Kiểm tra tất cả stages trong database
                    var allStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                        s => s.MethodId == batch.MethodId && !s.IsDeleted,
                        q => q.OrderBy(s => s.OrderIndex)
                    );
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: All stages for MethodId {batch.MethodId}: {string.Join(", ", allStages.Select(s => $"{s.StageId}({s.StageName})[Order:{s.OrderIndex}]"))}");
                    
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, $"Không tìm thấy stage với thứ tự: {failureInfo.FailedOrderIndex} trong danh sách stages của method {batch.MethodId}");
                }
                
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found failed stage: {failedStage.StageId} - {failedStage.StageName}");

                // 🔧 VALIDATION: Kiểm tra khối lượng output khi stage bị fail
                if (!input.OutputQuantity.HasValue || input.OutputQuantity.Value <= 0)
                {
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Khối lượng đầu ra phải lớn hơn 0 khi cải thiện stage bị fail.");
                }

                // Lấy progress cuối cùng để so sánh khối lượng
                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                var latestProgress = progresses.FirstOrDefault();
                int nextStepIndex = latestProgress != null ? latestProgress.StepIndex + 1 : 1;

                // 🔧 VALIDATION: So sánh khối lượng với progress trước đó (nếu có)
                if (latestProgress != null && latestProgress.OutputQuantity.HasValue)
                {
                    var previousQuantity = latestProgress.OutputQuantity.Value;
                    var currentQuantity = input.OutputQuantity.Value;
                    var improvementPercentage = ((currentQuantity - previousQuantity) / previousQuantity) * 100;

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Quantity comparison:");
                    Console.WriteLine($"  - Previous quantity: {previousQuantity} {latestProgress.OutputUnit}");
                    Console.WriteLine($"  - Current quantity: {currentQuantity} {input.OutputUnit}");
                    Console.WriteLine($"  - Improvement: {improvementPercentage:F2}%");

                    // 🔧 CẢI THIỆN: Thêm tolerance cho khối lượng (25% cho retry scenario)
                    const double tolerance = 0.25; // 25% tolerance cho retry
                    
                    if (currentQuantity > previousQuantity * (1 + tolerance))
                    {
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, 
                            $"Khối lượng đầu ra ({currentQuantity} {input.OutputUnit}) tăng quá nhiều so với lần trước ({previousQuantity} {latestProgress.OutputUnit}). " +
                            $"Vui lòng kiểm tra lại hoặc giải thích lý do tăng khối lượng.");
                    }

                    // Nếu khối lượng giảm quá nhiều (>20%), cảnh báo
                    if (improvementPercentage < -20)
                    {
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, 
                            $"Khối lượng đầu ra ({currentQuantity} {input.OutputUnit}) giảm quá nhiều so với lần trước ({previousQuantity} {latestProgress.OutputUnit}). " +
                            $"Vui lòng kiểm tra lại hoặc giải thích lý do giảm khối lượng.");
                    }
                }

                // 🔧 FIX: Bỏ validation sai - không thể so sánh InputQuantity với OutputQuantity
                // Vì InputQuantity = cà phê tươi, OutputQuantity = cà phê đã chế biến
                // Có thể có hao hụt hoặc thay đổi khối lượng trong quá trình chế biến
                
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Latest progress: {(latestProgress != null ? $"StepIndex: {latestProgress.StepIndex}, StageId: {latestProgress.StageId}" : "none")}");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Next StepIndex: {nextStepIndex}");
                
                // Kiểm tra StepIndex có trùng lặp không
                var existingStepIndex = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    p => p.BatchId == batchId && p.StepIndex == nextStepIndex && !p.IsDeleted
                );
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: StepIndex {nextStepIndex} already exists: {existingStepIndex}");

                // Tạo progress mới cho stage bị fail
                var progress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batchId,
                    StepIndex = nextStepIndex,
                    StageId = failedStage.StageId, // 🔧 FIX: Sử dụng StageId thực tế từ database
                    StageDescription = $"Cải thiện sau đánh giá fail - {failureInfo.FailureDetails}",
                    ProgressDate = input.ProgressDate,
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

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Creating progress with:");
                Console.WriteLine($"  - ProgressId: {progress.ProgressId}");
                Console.WriteLine($"  - BatchId: {progress.BatchId}");
                Console.WriteLine($"  - StepIndex: {progress.StepIndex}");
                Console.WriteLine($"  - StageId: {progress.StageId} (from OrderIndex {failureInfo.FailedOrderIndex})");
                Console.WriteLine($"  - StageName: {failedStage.StageName}");
                Console.WriteLine($"  - UpdatedBy: {progress.UpdatedBy}");
                Console.WriteLine($"  - OutputQuantity: {progress.OutputQuantity}");
                Console.WriteLine($"  - OutputUnit: {progress.OutputUnit}");

                // Lưu progress
                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);

                // Tạo parameters nếu có
                if (input.Parameters?.Any() == true)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Creating {input.Parameters.Count} parameters");
                    var parametersToCreate = input.Parameters.Select(p => new ProcessingParameter
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

                    foreach (var param in parametersToCreate)
                    {
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Creating parameter: {param.ParameterName} = {param.ParameterValue} {param.Unit}");
                        await _unitOfWork.ProcessingParameterRepository.CreateAsync(param);
                    }
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: All parameters created successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: No parameters to create");
                }

                // 🔧 QUAN TRỌNG: Chuyển status từ InProgress về AwaitingEvaluation
                batch.Status = "AwaitingEvaluation";
                batch.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                // Tạo evaluation mới cho expert đánh giá lại
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Creating new evaluation");
                
                // 🔧 FIX: Retry logic để tránh UNIQUE constraint violation
                string evaluationCode = null;
                int retryCount = 0;
                const int maxRetries = 5;
                
                while (evaluationCode == null && retryCount < maxRetries)
                {
                    try
                    {
                        var generatedCode = await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year);
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Generated evaluation code: {generatedCode} (attempt {retryCount + 1})");
                        
                        // Kiểm tra xem evaluation code đã tồn tại chưa
                        var existingEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByPredicateAsync(
                            predicate: e => e.EvaluationCode == generatedCode && !e.IsDeleted,
                            selector: e => e.EvaluationCode,
                            asNoTracking: true
                        );
                        
                        if (string.IsNullOrEmpty(existingEvaluation))
                        {
                            evaluationCode = generatedCode;
                            Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation code {evaluationCode} is unique");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation code {generatedCode} already exists, retrying...");
                            retryCount++;
                            await Task.Delay(100); // Đợi 100ms trước khi thử lại
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Error generating evaluation code: {ex.Message}");
                        retryCount++;
                        await Task.Delay(100);
                    }
                }
                
                if (evaluationCode == null)
                {
                    return new ServiceResult(Const.ERROR_EXCEPTION, "Không thể tạo mã đánh giá duy nhất sau nhiều lần thử.");
                }
                
                var newEvaluation = new ProcessingBatchEvaluation
                {
                    EvaluationId = Guid.NewGuid(),
                    EvaluationCode = evaluationCode,
                    BatchId = batchId,
                    EvaluatedBy = null,
                    EvaluatedAt = null,
                    EvaluationResult = null,
                    Comments = $"Đánh giá lại sau khi cải thiện stage: {failureInfo.FailedStageName}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(newEvaluation);
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: New evaluation created successfully with code: {evaluationCode}");

                // 🔧 QUAN TRỌNG: Lưu tất cả thay đổi một lần duy nhất
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: About to save changes...");
                try
                {
                    var saveResult = await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: SaveChangesAsync returned: {saveResult}");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: SaveChangesAsync failed: {saveEx.Message}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Inner exception: {saveEx.InnerException?.Message}");
                    throw; // Re-throw để service trả về lỗi
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Successfully updated progress and created new evaluation");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Final batch status: {batch.Status}");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Progress created with ID: {progress.ProgressId}");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation created with code: {evaluationCode}");

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, 
                    $"Đã cập nhật progress cho stage {failureInfo.FailedStageName} và chuyển sang chờ đánh giá lại.", 
                    progress.ProgressId);

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

        /// <summary>
        /// Lấy thông tin failure từ evaluation của batch
        /// </summary>
        /// <summary>
        /// Lấy thông tin failure từ evaluation của batch
        /// </summary>
        /// <param name="batchId">ID của batch</param>
        /// <returns>StageFailureInfo hoặc null nếu không có failure</returns>
        private async Task<StageFailureInfo?> GetFailureInfoForBatch(Guid batchId)
        {
            try
            {
                Console.WriteLine($"DEBUG: Getting failure info for batch: {batchId}");
                
                // Lấy evaluation cuối cùng của batch
                var latestEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId == batchId && !e.IsDeleted,
                    q => q.OrderByDescending(e => e.CreatedAt)
                );

                var evaluation = latestEvaluation.FirstOrDefault();
                if (evaluation == null)
                {
                    Console.WriteLine($"DEBUG: No evaluation found for batch: {batchId}");
                    return null;
                }
                
                if (evaluation.EvaluationResult != "Fail")
                {
                    Console.WriteLine($"DEBUG: Latest evaluation is not Fail. Result: {evaluation.EvaluationResult}");
                    return null;
                }

                Console.WriteLine($"DEBUG: Found Fail evaluation. Comments: {evaluation.Comments}");

                // Sử dụng StageFailureParser để parse comments
                var failureInfo = StageFailureParser.ParseFailureFromComments(evaluation.Comments);
                if (failureInfo != null)
                {
                    Console.WriteLine($"DEBUG: Parsed failure info - OrderIndex: {failureInfo.FailedOrderIndex}, StageName: {failureInfo.FailedStageName}");
                    
                    // Lấy StageId thực tế từ database dựa trên OrderIndex
                    var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                    if (batch != null)
                    {
                        var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                            s => s.MethodId == batch.MethodId && !s.IsDeleted,
                            q => q.OrderBy(s => s.OrderIndex)
                        );
                        
                        var failedStage = stages.FirstOrDefault(s => s.OrderIndex == failureInfo.FailedOrderIndex);
                        if (failedStage != null)
                        {
                            failureInfo.FailedStageId = failedStage.StageId;
                            Console.WriteLine($"DEBUG: Found actual StageId: {failedStage.StageId} for OrderIndex: {failureInfo.FailedOrderIndex}");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG: Warning - No stage found for OrderIndex: {failureInfo.FailedOrderIndex}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"DEBUG: Failed to parse failure comment: {evaluation.Comments}");
                }

                return failureInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error getting failure info: {ex.Message}");
                return null;
            }
        }
    }
}
