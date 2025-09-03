using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
// using DakLakCoffeeSupplyChain.APIService.Requests.ProcessingBatchProgressReques; // Moved to Common
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
        private readonly IEvaluationService _evaluationService;

        public ProcessingBatchProgressService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator, IEvaluationService evaluationService)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
            _evaluationService = evaluationService;
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi validation cho i18n
        /// </summary>
        private ServiceResult CreateValidationError(string errorKey, Dictionary<string, object> parameters = null)
        {
            var errorData = new
            {
                ErrorKey = errorKey,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                ErrorType = "ValidationError"
            };

            // Trả về errorKey trong message, data trong data để Frontend dễ xử lý
            return new ServiceResult(Const.ERROR_VALIDATION_CODE, errorKey, errorData);
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
                        return CreateValidationError("BusinessManagerNotFound");
                    }

                    var managerId = manager.ManagerId;


                    progresses = progresses
                        .Where(p => batchDict.ContainsKey(p.BatchId) &&
                                    batchDict[p.BatchId].CropSeason?.Commitment?.ApprovedBy == managerId)
                        .ToList();

                    if (!progresses.Any())
                    {
                        return CreateValidationError("NoPermissionToAccessAnyProgress");
                    }
                }
                else
                {

                    var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
                    if (farmer == null)
                        return CreateValidationError("FarmerNotFound");

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
                return CreateValidationError("ProgressNotFound", new Dictionary<string, object>
                {
                    ["ProgressId"] = progressId.ToString()
                });

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

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
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
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
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
                            return CreateValidationError("BusinessManagerNotFound", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString()
                            });
                        }

                        if (batch.CropSeason?.Commitment?.ApprovedBy != manager.ManagerId)
                        {
                            return CreateValidationError("NoPermissionToAccessBatch", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString(),
                                ["BatchId"] = batchId.ToString()
                            });
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
                            return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString()
                            });
                        }

                        if (batch.FarmerId != farmer.FarmerId)
                        {
                            return CreateValidationError("NoPermissionToAccessBatch", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString(),
                                ["BatchId"] = batchId.ToString()
                            });
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
                    return CreateValidationError("NoProgressesForBatch", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
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
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                // 2. Nếu không phải Admin hoặc Manager thì phải là đúng Farmer
                if (!isAdmin && !isManager)
                {
                    var farmer = (await _unitOfWork.FarmerRepository
                        .GetAllAsync(f => f.UserId == userId && !f.IsDeleted))
                        .FirstOrDefault();

                    if (farmer == null)
                        return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                        {
                            ["UserId"] = userId.ToString()
                        });

                    if (batch.FarmerId != farmer.FarmerId)
                        return CreateValidationError("NoPermissionToCreateProgress", new Dictionary<string, object>
                        {
                            ["UserId"] = userId.ToString(),
                            ["BatchId"] = batchId.ToString()
                        });
                }

                // 3. Kiểm tra khối lượng còn lại của batch
                var existingProgresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex))).ToList();

                // 🔧 VALIDATION: Kiểm tra ngày progress
                var dateValidationResult = await ValidateProgressDate(batchId, input.ProgressDate, existingProgresses);
                if (dateValidationResult.Status != Const.SUCCESS_READ_CODE)
                {
                    return dateValidationResult;
                }

                // 🔧 FIX: Bỏ logic tính remainingQuantity sai - không thể trừ InputQuantity với OutputQuantity
                // Vì InputQuantity = cà phê tươi, OutputQuantity = cà phê đã chế biến (đơn vị khác nhau)
                
                // 🔧 VALIDATION: Kiểm tra khối lượng output cơ bản
                if (input.OutputQuantity.HasValue)
                {
                    // Kiểm tra khối lượng phải > 0
                    if (input.OutputQuantity.Value <= 0)
                    {
                        return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                        {
                            ["OutputQuantity"] = input.OutputQuantity.Value,
                            ["MinValue"] = 0
                        });
                    }

                    // Kiểm tra khối lượng không được quá lớn (ví dụ: 100,000 kg)
                    if (input.OutputQuantity.Value > 100000)
                    {
                        return CreateValidationError("OutputQuantityTooLarge", new Dictionary<string, object>
                        {
                            ["OutputQuantity"] = input.OutputQuantity.Value,
                            ["MaxValue"] = 1000000
                        });
                    }

                    // 🔧 VALIDATION: Chỉ kiểm tra khối lượng ra với batch input cho tiến trình đầu tiên
                    if (!existingProgresses.Any())
                    {
                        // Chỉ kiểm tra nếu đơn vị giống nhau
                        var batchInputUnit = string.IsNullOrWhiteSpace(batch.InputUnit) ? "kg" : batch.InputUnit.Trim().ToLower();
                        var outputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();
                        
                        if (batchInputUnit == outputUnit && input.OutputQuantity.Value >= batch.InputQuantity)
                        {
                            return CreateValidationError("OutputQuantityExceedsInputQuantity", new Dictionary<string, object>
                            {
                                ["OutputQuantity"] = input.OutputQuantity.Value,
                                ["OutputUnit"] = input.OutputUnit ?? "kg",
                                ["InputQuantity"] = batch.InputQuantity,
                                ["InputUnit"] = batch.InputUnit
                            });
                        }
                    }

                    // Kiểm tra đơn vị hợp lệ
                    var validUnits = new[] { "kg", "g", "tấn", "lít", "ml", "bao", "thùng", "khác" };
                    var currentOutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();
                    
                    if (!validUnits.Contains(currentOutputUnit))
                    {
                        return CreateValidationError("InvalidOutputUnit", new Dictionary<string, object>
                        {
                            ["InvalidUnit"] = input.OutputUnit,
                            ["ValidUnits"] = string.Join(", ", validUnits)
                        });
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
                                return CreateValidationError("OutputQuantityIncreaseTooHigh", new Dictionary<string, object>
                                {
                                    ["CurrentQuantity"] = currentQuantity,
                                    ["CurrentUnit"] = input.OutputUnit ?? batch.InputUnit,
                                    ["PreviousQuantity"] = previousQuantity,
                                    ["PreviousUnit"] = latestProgress.OutputUnit,
                                    ["Tolerance"] = tolerance * 100,
                                    ["IncreasePercentage"] = changePercentage
                                });
                            }

                            // Nếu khối lượng giảm quá nhiều (>70%), cảnh báo
                            if (changePercentage < -70)
                            {
                                return CreateValidationError("OutputQuantityDecreaseTooHigh", new Dictionary<string, object>
                                {
                                    ["CurrentQuantity"] = currentQuantity,
                                    ["CurrentUnit"] = input.OutputUnit ?? batch.InputUnit,
                                    ["PreviousQuantity"] = previousQuantity,
                                    ["PreviousUnit"] = latestProgress.OutputUnit,
                                    ["DecreasePercentage"] = Math.Abs(changePercentage)
                                });
                            }
                        }
                    }
                }

                // 4. Lấy danh sách công đoạn (stage) theo MethodId
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (!stages.Any())
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"] = batch.MethodId.ToString()
                    });

                // 5. Kiểm tra xem batch có bị Fail không (đã chuyển sang đánh giá batch toàn diện)
                var failureInfo = await GetFailureInfoForBatch(batchId);
                if (failureInfo != null)
                {
                    Console.WriteLine($"DEBUG CREATE: Found failure info for batch - BatchId: {batchId}");
                }
                else
                {
                    Console.WriteLine($"DEBUG CREATE: No failure info found");
                }
                
                // 6. Xác định bước tiếp theo (hỗ trợ retry stage khi batch fail)
                int nextStepIndex = 0;
                int nextStageId = 0;
                bool isLastStep = false;
                bool isRetryScenario = false;

                if (!existingProgresses.Any())
                {
                    // Bước đầu tiên
                    nextStageId = stages[0].StageId;
                    nextStepIndex = 1;
                    isLastStep = (stages.Count == 1);
                }
                else
                {
                    // Tìm bước tiếp theo
                    var latestProgress = existingProgresses.Last();
                    var currentStageIndex = stages.FindIndex(s => s.StageId == latestProgress.StageId);

                    if (currentStageIndex == -1)
                        return CreateValidationError("CurrentStageNotFound", new Dictionary<string, object>
                        {
                            ["StageId"] = latestProgress.StageId.ToString()
                        });

                                        // 🔧 KIỂM TRA RETRY SCENARIO: Nếu batch bị fail, cho phép retry stage cuối cùng
                    if (failureInfo != null)
                    {
                        var failureInfoObj = failureInfo as dynamic;
                        if (failureInfoObj?.FailedStageId != null && latestProgress.StageId == failureInfoObj.FailedStageId)
                    {
                        // ✅ Retry stage hiện tại (stage bị fail)
                        nextStageId = latestProgress.StageId;
                        nextStepIndex = latestProgress.StepIndex + 1;
                        isLastStep = (currentStageIndex == stages.Count - 1);
                        isRetryScenario = true;
                        
                        Console.WriteLine($"DEBUG: Retry scenario - StageId: {nextStageId}, StepIndex: {nextStepIndex}, isLastStep: {isLastStep}");
                    }
                    }

                    // 🔧 ĐẢM BẢO LUÔN CÓ GIÁ TRỊ: Nếu không phải retry scenario thì xử lý bình thường
                    if (!isRetryScenario)
                    {
                        if (currentStageIndex >= stages.Count - 1)
                        {
                            // Đã hoàn thành tất cả stages
                            return CreateValidationError("CannotCreateNextStepLastStageCompleted", new Dictionary<string, object>
                            {
                                ["StageName"] = stages[currentStageIndex].StageName
                            });
                    }
                    else
                    {
                        // Bước tiếp theo bình thường
                        var nextStage = stages[currentStageIndex + 1];
                        nextStageId = nextStage.StageId;
                        nextStepIndex = latestProgress.StepIndex + 1;
                            isLastStep = (nextStage.OrderIndex >= stages.Max(s => s.OrderIndex));
                        
                        Console.WriteLine($"DEBUG: Normal next step - CurrentStageIndex: {currentStageIndex}, StagesCount: {stages.Count}, isLastStep: {isLastStep}");
                        Console.WriteLine($"DEBUG: NextStageId: {nextStageId}, NextStepIndex: {nextStepIndex}");
                        }
                    }
                }

                // 🔧 VALIDATION: Đảm bảo nextStageId và nextStepIndex đã được gán giá trị
                if (nextStageId == 0)
                {
                    Console.WriteLine($"ERROR: nextStageId is not assigned! This should not happen.");
                    return CreateValidationError("InternalError", new Dictionary<string, object>
                    {
                        ["Message"] = "Lỗi nội bộ: Không thể xác định stage tiếp theo"
                    });
                }

                // 7. VALIDATION: Kiểm tra nếu input có StageId thì phải đúng Stage được phép
                if (input.StageId.HasValue && input.StageId.Value != nextStageId)
                {
                    var requestedStage = stages.FirstOrDefault(s => s.StageId == input.StageId.Value);
                    var allowedStage = stages.FirstOrDefault(s => s.StageId == nextStageId);
                    
                    return CreateValidationError("InvalidStageForNextStep", new Dictionary<string, object>
                    {
                        ["RequestedStageName"] = requestedStage?.StageName ?? "Unknown",
                        ["AllowedStageName"] = allowedStage?.StageName ?? "Unknown",
                        ["AllowedOrderIndex"] = allowedStage?.OrderIndex ?? 0
                    });
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
                Console.WriteLine($"DEBUG CREATE: Processing workflow - isLastStep: {isLastStep}");
                
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
                return CreateValidationError("ProgressNotFound", new Dictionary<string, object>
                {
                    ["ProgressId"] = progressId.ToString()
                });

            // [Step 1.5] Kiểm tra batch status - chỉ cho phép update khi batch đang InProgress
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(entity.BatchId);
            if (batch == null)
                return CreateValidationError("BatchNotFoundForProgress", new Dictionary<string, object>
                {
                    ["BatchId"] = entity.BatchId.ToString()
                });
            
            if (batch.Status != "InProgress")
                return CreateValidationError("CannotUpdateProgressBatchNotInProgress", new Dictionary<string, object>
                {
                    ["CurrentStatus"] = batch.Status
                });

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
                    return CreateValidationError("StepIndexAlreadyExists", new Dictionary<string, object>
                    {
                        ["StepIndex"] = dto.StepIndex,
                        ["BatchId"] = entity.BatchId.ToString()
                    });
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
                return CreateValidationError("NoDataModified", new Dictionary<string, object>
                {
                    ["ProgressId"] = progressId.ToString()
                });
            }

            entity.UpdatedAt = DateTime.UtcNow;

            // [Step 4] Gọi UpdateAsync và kiểm tra trả về bool
            var updated = await _unitOfWork.ProcessingBatchProgressRepository.UpdateAsync(entity);
            if (!updated)
                return CreateValidationError("UpdateFailed", new Dictionary<string, object>
                {
                    ["ProgressId"] = progressId.ToString()
                });
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                var resultDto = entity.MapToProcessingBatchProgressDetailDto();

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, resultDto);
            }
            else
            {
                return CreateValidationError("NoChangesSaved", new Dictionary<string, object>
                {
                    ["ProgressId"] = progressId.ToString()
                });
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
                    return CreateValidationError("ProgressNotFoundOrAlreadyDeleted", new Dictionary<string, object>
                    {
                        ["ProgressId"] = progressId.ToString()
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
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
                    return CreateValidationError("InvalidBatchId", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });

                // if (isAdmin || isManager)
                //     return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Chỉ nông hộ mới được phép cập nhật tiến trình.");

                // Lấy Farmer từ userId
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Looking for farmer with userId: {userId}");
                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId == userId && !f.IsDeleted)).FirstOrDefault();
                if (farmer == null)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Farmer not found for userId: {userId}");
                    return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                    {
                        ["UserId"] = userId.ToString()
                    });
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found farmer: {farmer.FarmerId}");

                // Lấy Batch
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting batch: {batchId}");
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Batch not found or deleted: {batchId}");
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found batch: {batch.BatchId}, status: {batch.Status}, farmerId: {batch.FarmerId}");

                if (batch.FarmerId != farmer.FarmerId)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Permission denied - batch farmer: {batch.FarmerId}, current farmer: {farmer.FarmerId}");
                    return CreateValidationError("NoPermissionToUpdateBatch", new Dictionary<string, object>
                    {
                        ["UserId"] = userId.ToString(),
                        ["BatchId"] = batchId.ToString()
                    });
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
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"] = batch.MethodId.ToString()
                    });
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
                        return CreateValidationError("CurrentStageNotFound", new Dictionary<string, object>
                        {
                            ["StageId"] = latestProgress.StageId.ToString()
                        });
                    }

                    if (currentStageIdx >= stages.Count - 1)
                    {
                        // Đã ở stage cuối cùng
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Already at last stage, cannot advance further");
                        return CreateValidationError("AllStepsCompletedCannotAdvance", new Dictionary<string, object>
                        {
                            ["CurrentStageName"] = stages[currentStageIdx].StageName
                        });
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
                    return CreateValidationError("NextStageNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });

                // 🔧 VALIDATION: Kiểm tra khối lượng output khi advance
                if (input.OutputQuantity.HasValue)
                {
                    // Kiểm tra khối lượng phải > 0
                    if (input.OutputQuantity.Value <= 0)
                    {
                        return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                        {
                            ["OutputQuantity"] = input.OutputQuantity.Value,
                            ["MinValue"] = 0
                        });
                    }

                    // 🔧 VALIDATION: Chỉ kiểm tra khối lượng ra với batch input cho tiến trình đầu tiên (advance)
                    // Vì advance luôn có progress trước đó nên không cần kiểm tra này
                    // Chỉ kiểm tra nếu đơn vị giống nhau và là tiến trình đầu tiên
                    var batchInputUnit = string.IsNullOrWhiteSpace(batch.InputUnit) ? "kg" : batch.InputUnit.Trim().ToLower();
                    var advanceOutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();
                    
                    if (batchInputUnit == advanceOutputUnit && input.OutputQuantity.Value >= batch.InputQuantity)
                    {
                        return CreateValidationError("OutputQuantityExceedsInputQuantity", new Dictionary<string, object>
                        {
                            ["OutputQuantity"] = input.OutputQuantity.Value,
                            ["OutputUnit"] = input.OutputUnit ?? "kg",
                            ["InputQuantity"] = batch.InputQuantity,
                            ["InputUnit"] = batch.InputUnit
                        });
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
                            return CreateValidationError("OutputQuantityIncreaseTooHigh", new Dictionary<string, object>
                            {
                                ["CurrentQuantity"] = currentQuantity,
                                ["CurrentUnit"] = input.OutputUnit ?? "kg",
                                ["PreviousQuantity"] = previousQuantity,
                                ["PreviousUnit"] = latestProgress.OutputUnit,
                                ["Tolerance"] = tolerance * 100,
                                ["IncreasePercentage"] = changePercentage
                            });
                        }

                        // Nếu khối lượng giảm quá nhiều (>70%), cảnh báo
                        if (changePercentage < -70)
                        {
                            return CreateValidationError("OutputQuantityDecreaseTooHigh", new Dictionary<string, object>
                            {
                                ["CurrentQuantity"] = currentQuantity,
                                ["CurrentUnit"] = input.OutputUnit ?? "kg",
                                ["PreviousQuantity"] = previousQuantity,
                                ["PreviousUnit"] = latestProgress.OutputUnit,
                                ["DecreasePercentage"] = Math.Abs(changePercentage)
                            });
                        }
                    }
                }

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Creating new progress - stepIndex: {nextStepIndex}, stageId: {nextStage.StageId}");
                var newProgress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batch.BatchId,
                    StepIndex = nextStepIndex,
                    StageId = nextStage.StageId,
                    StageDescription = !string.IsNullOrWhiteSpace(input.StageDescription) ? input.StageDescription : (nextStage.Description ?? ""),
                    ProgressDate = input.ProgressDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
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
                            return CreateValidationError("CannotCreateNextStep", new Dictionary<string, object>
                            {
                                ["BatchId"] = batchId.ToString()
                            });
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
                    return CreateValidationError("InvalidBatchId", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });

                // Lấy Farmer từ userId
                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId == userId && !f.IsDeleted)).FirstOrDefault();
                if (farmer == null)
                {
                    return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                    {
                        ["UserId"] = userId.ToString()
                    });
                }

                // Lấy Batch
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null || batch.IsDeleted)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                if (batch.FarmerId != farmer.FarmerId)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Permission denied - batch farmer: {batch.FarmerId}, current farmer: {farmer.FarmerId}");
                    return CreateValidationError("NoPermissionToUpdateBatch", new Dictionary<string, object>
                    {
                        ["UserId"] = userId.ToString(),
                        ["BatchId"] = batchId.ToString()
                    });
                }
                
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Permission granted - using farmer: {farmer.FarmerId}");
                
                // Kiểm tra farmer có tồn tại trong database không
                var farmerExists = await _unitOfWork.FarmerRepository.AnyAsync(f => f.FarmerId == farmer.FarmerId && !f.IsDeleted);
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Farmer exists in database: {farmerExists}");

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Batch status: '{batch.Status}'");
                
                // 🔧 KIỂM TRA: Chỉ cho phép cập nhật khi batch đang ở InProgress (sau khi bị fail)
                if (batch.Status != "InProgress")
                {
                    return CreateValidationError("CannotUpdateProgressBatchNotInProgress", new Dictionary<string, object>
                    {
                        ["CurrentStatus"] = batch.Status
                    });
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
                    return CreateValidationError("NoEvaluationFoundForBatch", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }
                
                if (evaluation.EvaluationResult != "Fail")
                {
                    return CreateValidationError("LastEvaluationNotFail", new Dictionary<string, object>
                    {
                        ["CurrentResult"] = evaluation.EvaluationResult ?? "Unknown"
                    });
                }

                // Lấy danh sách stages bị fail từ SystemConfiguration
                var failedStages = await _evaluationService.GetFailedStagesForBatchAsync(batchId);
                if (failedStages == null || failedStages.Count == 0)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: No failed stages found for batch {batchId}");
                    return CreateValidationError("NoFailedStagesToUpdate", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {failedStages.Count} failed stages: {string.Join(", ", failedStages)}");

                // Lấy danh sách stages
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count == 0)
                {
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"] = batch.MethodId.ToString()
                    });
                }

                // Kiểm tra xem stage được cập nhật có trong danh sách failed stages không
                var currentStage = stages.FirstOrDefault(s => s.StageId == input.StageId);
                if (currentStage == null)
                {
                    return CreateValidationError("CurrentStageNotFound", new Dictionary<string, object>
                    {
                        ["StageId"] = input.StageId?.ToString() ?? "null",
                        ["MethodId"] = batch.MethodId.ToString()
                    });
                }

                // Kiểm tra xem stage hiện tại có trong danh sách failed stages không
                var stageNameWithOrder = $"{currentStage.StageName} (Thứ tự: {currentStage.OrderIndex})";
                var isStageInFailedList = failedStages.Any(failedStage => 
                    failedStage.Contains(currentStage.StageName) || 
                    failedStage.Contains(stageNameWithOrder) ||
                    failedStage.Contains(currentStage.StageId.ToString())
                );

                if (!isStageInFailedList)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Stage {currentStage.StageName} (ID: {currentStage.StageId}) is not in failed stages list");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Failed stages: {string.Join(", ", failedStages)}");
                    return CreateValidationError("StageNotInFailedList", new Dictionary<string, object>
                    {
                        ["StageId"] = currentStage.StageId.ToString(),
                        ["StageName"] = currentStage.StageName,
                        ["FailedStages"] = string.Join(", ", failedStages)
                    });
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Stage {currentStage.StageName} (ID: {currentStage.StageId}) is in failed stages list - proceeding with update");

                // 🔧 VALIDATION: Kiểm tra khối lượng output (đã chuyển sang đánh giá batch toàn diện)
                if (!input.OutputQuantity.HasValue || input.OutputQuantity.Value <= 0)
                {
                    return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                    {
                        ["OutputQuantity"] = input.OutputQuantity?.ToString() ?? "null",
                        ["MinValue"] = 0
                    });
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
                        return CreateValidationError("OutputQuantityIncreaseTooHigh", new Dictionary<string, object>
                        {
                            ["CurrentQuantity"] = currentQuantity,
                            ["CurrentUnit"] = input.OutputUnit ?? "kg",
                            ["PreviousQuantity"] = previousQuantity,
                            ["PreviousUnit"] = latestProgress.OutputUnit,
                            ["Tolerance"] = tolerance * 100,
                            ["IncreasePercentage"] = improvementPercentage
                        });
                    }

                    // Nếu khối lượng giảm quá nhiều (>20%), cảnh báo
                    if (improvementPercentage < -20)
                    {
                        return CreateValidationError("OutputQuantityDecreaseTooHigh", new Dictionary<string, object>
                        {
                            ["CurrentQuantity"] = currentQuantity,
                            ["CurrentUnit"] = input.OutputUnit ?? "kg",
                            ["PreviousQuantity"] = previousQuantity,
                            ["PreviousUnit"] = latestProgress.OutputUnit,
                            ["DecreasePercentage"] = Math.Abs(improvementPercentage)
                        });
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

                // Tạo progress mới cho stage hiện tại
                var progress = new ProcessingBatchProgress
                {
                    ProgressId = Guid.NewGuid(),
                    BatchId = batchId,
                    StepIndex = nextStepIndex,
                    StageId = currentStage.StageId,
                    StageDescription = $"Cải thiện sau đánh giá fail - batch evaluation",
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
                                    Console.WriteLine($"  - StageId: {progress.StageId}");
                Console.WriteLine($"  - StageName: {currentStage.StageName}");
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

                // 🔧 MỚI: Cập nhật evaluation hiện có thay vì tạo mới
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Updating existing evaluation for re-evaluation");
                
                // Lấy evaluation hiện có để cập nhật (sử dụng GetAllAsync để có thể sắp xếp)
                var existingEvaluations = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId == batchId && !e.IsDeleted,
                    null,
                    q => q.OrderByDescending(e => e.CreatedAt),
                    false
                );
                
                var existingEvaluation = existingEvaluations.FirstOrDefault();
                
                if (existingEvaluation != null)
                {
                    // Cập nhật evaluation hiện có để expert đánh giá lại
                    existingEvaluation.EvaluationResult = null; // Reset kết quả để đánh giá lại
                    existingEvaluation.EvaluatedBy = null; // Reset expert để đánh giá lại
                    existingEvaluation.EvaluatedAt = null; // Reset thời gian đánh giá
                    existingEvaluation.Comments = $"Đánh giá lại sau khi cải thiện batch - {existingEvaluation.Comments}";
                    existingEvaluation.UpdatedAt = DateTime.UtcNow;
                    
                    await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(existingEvaluation);
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Updated existing evaluation: {existingEvaluation.EvaluationId}");
                }
                else
                {
                    // Fallback: tạo evaluation mới nếu không tìm thấy evaluation hiện có
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: No existing evaluation found, creating new one");
                    
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
                            var existingEvaluationCode = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByPredicateAsync(
                                predicate: e => e.EvaluationCode == generatedCode && !e.IsDeleted,
                                selector: e => e.EvaluationCode,
                                asNoTracking: true
                            );
                            
                            if (string.IsNullOrEmpty(existingEvaluationCode))
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
                        return CreateValidationError("CannotGenerateUniqueEvaluationCode", new Dictionary<string, object>
                        {
                            ["MaxRetries"] = maxRetries
                        });
                    }
                    
                    var newEvaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        EvaluationCode = evaluationCode,
                        BatchId = batchId,
                        EvaluatedBy = null,
                        EvaluatedAt = null,
                        EvaluationResult = null,
                        Comments = $"Đánh giá lại sau khi cải thiện batch",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(newEvaluation);
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: New evaluation created successfully with code: {evaluationCode}");
                }

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
               

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, 
                    $"Đã cập nhật progress và chuyển sang chờ đánh giá lại batch trên cùng evaluation.", 
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
                        return CreateValidationError("BusinessManagerNotFound", new Dictionary<string, object>
                        {
                            ["UserId"] = userId.ToString()
                        });

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
                        return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                        {
                            ["UserId"] = userId.ToString()
                        });

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
                    return CreateValidationError("NoAvailableBatchesForProgress", new Dictionary<string, object>
                    {
                        ["UserId"] = userId.ToString()
                    });
                }

                // Tính toán khối lượng còn lại cho mỗi batch
                var result = new List<AvailableBatchForProgressDto>();
                foreach (var batch in availableBatches)
                {
                    // 🔧 FIX: Lấy OutputQuantity của bước cuối cùng (StepIndex cao nhất)
                    // Vì bước cuối mới là sản lượng thực tế cuối cùng
                    var finalProgress = batch.ProcessingBatchProgresses
                        .Where(p => p.OutputQuantity.HasValue && p.OutputQuantity.Value > 0)
                        .OrderByDescending(p => p.StepIndex)  // Tìm StepIndex cao nhất
                        .FirstOrDefault();
                    var finalOutputQuantity = finalProgress?.OutputQuantity ?? 0;

                    // Khối lượng còn lại = InputQuantity - finalOutputQuantity
                    var remainingQuantity = batch.InputQuantity - finalOutputQuantity;

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
                            TotalProcessedQuantity = finalOutputQuantity,
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

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, result);
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
                        return CreateValidationError("BusinessManagerNotFound");
                    }

                        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(b => b.BatchId == batchId && !b.IsDeleted);
                        if (batch == null)
                        {
                            return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                        {
                            ["BatchId"] = batchId.ToString()
                        });
                        }

                        var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(c => c.CommitmentId == batch.CropSeason.CommitmentId && !c.IsDeleted);
                        if (commitment?.ApprovedBy != manager.ManagerId)
                        {
                            return CreateValidationError("NoPermissionToAccessBatch", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString(),
                                ["BatchId"] = batchId.ToString()
                            });
                        }
                    }
                    else
                    {
                        var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);
                        if (farmer == null)
                        {
                            return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString()
                            });
                        }

                        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(b => b.BatchId == batchId && b.FarmerId == farmer.FarmerId && !b.IsDeleted);
                        if (batch == null)
                        {
                            return CreateValidationError("BatchNotFoundOrNoPermission", new Dictionary<string, object>
                            {
                                ["UserId"] = userId.ToString(),
                                ["BatchId"] = batchId.ToString()
                            });
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
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                // Kiểm tra trạng thái batch
                if (processingBatch.Status != ProcessingStatus.InProgress.ToString() && 
                    processingBatch.Status != ProcessingStatus.NotStarted.ToString())
                {
                    return CreateValidationError("BatchNotInProgressableState", new Dictionary<string, object>
                    {
                        ["CurrentStatus"] = processingBatch.Status
                    });
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
                    return CreateValidationError("NextStepInfoNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString(),
                        ["NextStepIndex"] = nextStepIndex
                    });
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

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, newProgress);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi tiến hành bước tiếp theo: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy thông tin failure từ evaluation của batch và xác định stage cần retry
        /// </summary>
        /// <param name="batchId">ID của batch</param>
        /// <returns>Thông tin failure và stage cần retry hoặc null nếu không có failure</returns>
        private async Task<object?> GetFailureInfoForBatch(Guid batchId)
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

                // Lấy batch để biết method
                    var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                    {
                    Console.WriteLine($"DEBUG: Batch not found: {batchId}");
                    return null;
                }

                // Lấy tất cả stages của method
                        var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                            s => s.MethodId == batch.MethodId && !s.IsDeleted,
                            q => q.OrderBy(s => s.OrderIndex)
                        );
                        
                // Lấy tất cả progress đã thực hiện
                var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex)
                );

                // Xác định stage cuối cùng đã thực hiện để retry
                var lastProgress = progresses.LastOrDefault();
                var stageToRetry = lastProgress != null ? stages.FirstOrDefault(s => s.StageId == lastProgress.StageId) : null;

                // Trả về thông tin failure và stage cần retry
                var failureInfo = new
                {
                    BatchId = batchId,
                    EvaluationId = evaluation.EvaluationId,
                    FailedAt = evaluation.CreatedAt,
                    Comments = evaluation.Comments,
                    // Thông tin stage cần retry
                    FailedStageId = stageToRetry?.StageId,
                    FailedStageName = stageToRetry?.StageName,
                    FailedOrderIndex = stageToRetry?.OrderIndex,
                    LastStepIndex = lastProgress?.StepIndex ?? 0,
                    // Thông tin tất cả stages đã thực hiện
                    CompletedStages = progresses.Select(p => new
                    {
                        StageId = p.StageId,
                        StageName = stages.FirstOrDefault(s => s.StageId == p.StageId)?.StageName,
                        OrderIndex = stages.FirstOrDefault(s => s.StageId == p.StageId)?.OrderIndex,
                        StepIndex = p.StepIndex,
                        OutputQuantity = p.OutputQuantity,
                        OutputUnit = p.OutputUnit,
                        ProgressDate = p.ProgressDate
                    }).ToList(),
                    Note = "Batch bị fail - cần retry stage cuối cùng đã thực hiện"
                };

                Console.WriteLine($"DEBUG: Created failure info with retry stage: {stageToRetry?.StageName} (ID: {stageToRetry?.StageId})");
                return failureInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error getting failure info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tạo progress với media và waste
        /// </summary>
        public async Task<IServiceResult> CreateWithMediaAndWasteAsync(Guid batchId, ProcessingBatchProgressCreateRequest input, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                // 🔍 DEBUG: Log chi tiết về waste data
                Console.WriteLine($"🔍 Service: Starting create with media and waste for batchId: {batchId}");
                Console.WriteLine($"🔍 Service: Input Wastes count: {input.Wastes?.Count ?? 0}");
                Console.WriteLine($"🔍 Service: Input WasteType: {input.WasteType}");
                Console.WriteLine($"🔍 Service: Input WasteQuantity: {input.WasteQuantity}");
                Console.WriteLine($"🔍 Service: Input WasteUnit: {input.WasteUnit}");
                Console.WriteLine($"🔍 Service: Input WasteNote: {input.WasteNote}");
                Console.WriteLine($"🔍 Service: Input WasteRecordedAt: {input.WasteRecordedAt}");
                
                // 1. 🔧 VALIDATION: Chỉ validate những gì người dùng nhập vào
                var hasOutputQuantity = input.OutputQuantity.HasValue && input.OutputQuantity.Value > 0;
                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit)) ||
                                   (input.Wastes?.Any() == true);
                
                // Validate khối lượng nếu có
                if (hasOutputQuantity)
                {
                    var outputQuantityValidationResult = await ValidateOutputQuantityBeforeCreateProgress(batchId, input);
                    if (outputQuantityValidationResult.Status != Const.SUCCESS_READ_CODE)
                    {
                        return outputQuantityValidationResult;
                    }
                }
                
                // Validate waste nếu có
                if (hasWasteData)
                {
                    var wasteValidationResult = await ValidateWasteBeforeCreateProgress(batchId, input);
                    if (wasteValidationResult.Status != Const.SUCCESS_READ_CODE)
                    {
                        return wasteValidationResult;
                    }
                }
                
                // 2. Parse parameters từ request
                var parameters = await ParseParametersFromRequest(input);
                
                // 3. Tạo progress DTO
                var progressDto = new ProcessingBatchProgressCreateDto
                {
                    StageId = input.StageId,
                    ProgressDate = input.ProgressDate,
                    OutputQuantity = input.OutputQuantity,
                    OutputUnit = input.OutputUnit,
                    PhotoUrl = null,
                    VideoUrl = null,
                    Parameters = parameters.Any() ? parameters : null
                };

                // 4. Tạo progress (đã validate waste trước)
                var progressResult = await CreateAsync(batchId, progressDto, userId, isAdmin, isManager);
                if (progressResult.Status != Const.SUCCESS_CREATE_CODE)
                {
                    return progressResult;
                }

                var progressId = (Guid)progressResult.Data;

                // 5. Tạo waste nếu có - từ field riêng biệt hoặc từ array (sau khi đã validate output quantity và waste)
                var createdWastes = new List<ProcessingWasteViewAllDto>();
                Console.WriteLine($"🔍 Service: Input Wastes count: {input.Wastes?.Count ?? 0}");
                
                // Kiểm tra waste từ field riêng biệt trước
                if (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit))
                {
                    Console.WriteLine($"🔍 Service: Creating waste from individual fields - Type: {input.WasteType}, Quantity: {input.WasteQuantity}, Unit: {input.WasteUnit}");
                    var wasteDto = new ProcessingWasteCreateDto
                    {
                        WasteType = input.WasteType,
                        Quantity = input.WasteQuantity.Value,
                        Unit = input.WasteUnit,
                        Note = input.WasteNote,
                        RecordedAt = input.WasteRecordedAt ?? DateTime.UtcNow
                    };
                    var wasteList = new List<ProcessingWasteCreateDto> { wasteDto };
                    createdWastes = await CreateWastesForProgress(wasteList, progressId, userId, isAdmin);
                    Console.WriteLine($"🔍 Service: Created waste from individual fields, count: {createdWastes.Count}");
                }
                // Nếu không có field riêng biệt, kiểm tra array
                else if (input.Wastes?.Any() == true)
                {
                    Console.WriteLine($"🔍 Service: About to create wastes from array for progressId: {progressId}");
                    createdWastes = await CreateWastesForProgress(input.Wastes, progressId, userId, isAdmin);
                    Console.WriteLine($"🔍 Service: Created wastes from array, count: {createdWastes.Count}");
                }

                // 6. Tạo response parameters từ input (tránh gọi GetByIdAsync gây conflict)
                var responseParameters = new List<ProcessingParameterViewAllDto>();
                if (parameters.Any())
                {
                    responseParameters = parameters.Select(p => new ProcessingParameterViewAllDto
                    {
                        ParameterId = Guid.NewGuid(), // Tạm thời, sẽ được cập nhật khi lấy từ DB
                        ProgressId = progressId,
                        ParameterName = p.ParameterName,
                        ParameterValue = p.ParameterValue,
                        Unit = p.Unit,
                        RecordedAt = p.RecordedAt
                    }).ToList();
                }

                // 7. Tạo response DTO
                var response = new ProcessingBatchProgressMediaResponse
                {
                    Message = progressResult.Message,
                    ProgressId = progressId,
                    PhotoUrl = null,
                    VideoUrl = null,
                    MediaCount = 0,
                    AllPhotoUrls = new List<string>(),
                    AllVideoUrls = new List<string>(),
                    Parameters = responseParameters,
                    Wastes = createdWastes
                };

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, response);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi tạo progress với waste: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse parameters từ request
        /// </summary>
        private async Task<List<ProcessingParameterInProgressDto>> ParseParametersFromRequest(ProcessingBatchProgressCreateRequest request)
        {
            var parameters = new List<ProcessingParameterInProgressDto>();
            
            // Single parameter
            if (!string.IsNullOrEmpty(request.ParameterName))
            {
                parameters.Add(new ProcessingParameterInProgressDto
                {
                    ParameterName = request.ParameterName,
                    ParameterValue = request.ParameterValue,
                    Unit = request.Unit,
                    RecordedAt = request.RecordedAt
                });
            }
            
            // Multiple parameters từ JSON array
            if (!string.IsNullOrEmpty(request.ParametersJson))
            {
                try
                {
                    var multipleParams = System.Text.Json.JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(request.ParametersJson);
                    if (multipleParams != null)
                    {
                        parameters.AddRange(multipleParams);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing parameters JSON: {ex.Message}");
                }
            }
            
            return parameters;
        }

        /// <summary>
        /// Tạo waste cho progress
        /// </summary>
        private async Task<List<ProcessingWasteViewAllDto>> CreateWastesForProgress(List<ProcessingWasteCreateDto> wasteDtos, Guid progressId, Guid userId, bool isAdmin)
        {
            Console.WriteLine($"🔍 CreateWastesForProgress: Starting with {wasteDtos.Count} wastes");
            
            var createdWastes = new List<ProcessingWasteViewAllDto>();
            
            foreach (var wasteDto in wasteDtos)
            {
                Console.WriteLine($"🔍 CreateWastesForProgress: Processing waste - Type: {wasteDto.WasteType}, Quantity: {wasteDto.Quantity}, Unit: {wasteDto.Unit}");
                // Gán ProgressId cho waste
                wasteDto.ProgressId = progressId;
                
                // Tạo waste entity
                var wasteEntity = new ProcessingBatchWaste
                {
                    WasteId = Guid.NewGuid(),
                    WasteCode = await _codeGenerator.GenerateProcessingWasteCodeAsync(),
                    ProgressId = wasteDto.ProgressId,
                    WasteType = wasteDto.WasteType,
                    Quantity = wasteDto.Quantity,
                    Unit = wasteDto.Unit,
                    Note = wasteDto.Note,
                    RecordedAt = wasteDto.RecordedAt ?? DateTime.UtcNow,
                    RecordedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    IsDisposed = false 
                };

                // Gọi service tạo waste
                await _unitOfWork.ProcessingWasteRepository.CreateAsync(wasteEntity);

                // Map to DTO
                var wasteViewDto = new ProcessingWasteViewAllDto
                {
                    WasteId = wasteEntity.WasteId,
                    WasteCode = wasteEntity.WasteCode,
                    ProgressId = wasteEntity.ProgressId,
                    WasteType = wasteEntity.WasteType,
                    Quantity = wasteEntity.Quantity ?? 0,
                    Unit = wasteEntity.Unit,
                    Note = wasteEntity.Note,
                    RecordedAt = wasteEntity.RecordedAt.HasValue ? DateOnly.FromDateTime(wasteEntity.RecordedAt.Value) : null,
                    RecordedBy = wasteEntity.RecordedBy?.ToString() ?? "",
                    IsDisposed = wasteEntity.IsDisposed ?? false, 
                    DisposedAt = wasteEntity.DisposedAt,
                    CreatedAt = wasteEntity.CreatedAt,
                    UpdatedAt = wasteEntity.UpdatedAt
                };
                
                createdWastes.Add(wasteViewDto);
            }
            
            Console.WriteLine($"🔍 CreateWastesForProgress: About to commit {createdWastes.Count} wastes to database");
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"🔍 CreateWastesForProgress: Successfully committed wastes to database");
            return createdWastes;
        }

        /// <summary>
        /// Advance progress với media và waste
        /// </summary>
        public async Task<IServiceResult> AdvanceWithMediaAndWasteAsync(Guid batchId, AdvanceProcessingBatchProgressRequest input, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                // 🔍 DEBUG: Log chi tiết về waste data trong advance service
                Console.WriteLine($"🔍 ADVANCE SERVICE: Starting advance for batchId: {batchId}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input Wastes count: {input.Wastes?.Count ?? 0}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteType: {input.WasteType}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteQuantity: {input.WasteQuantity}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteUnit: {input.WasteUnit}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteNote: {input.WasteNote}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteRecordedAt: {input.WasteRecordedAt}");
                
                // 1. 🔧 VALIDATION: Kiểm tra ngày progress cho advance
                var existingProgresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex))).ToList();

                var dateValidationResult = await ValidateProgressDate(batchId, input.ProgressDate, existingProgresses);
                if (dateValidationResult.Status != Const.SUCCESS_READ_CODE)
                {
                    return dateValidationResult;
                }

                // 2. 🔧 VALIDATION: Chỉ validate những gì người dùng nhập vào
                var hasOutputQuantity = input.OutputQuantity.HasValue && input.OutputQuantity.Value > 0;
                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit)) ||
                                   (input.Wastes?.Any() == true);
                
                // Validate khối lượng nếu có
                if (hasOutputQuantity)
                {
                    var outputQuantityValidationResult = await ValidateOutputQuantityBeforeAdvanceProgress(batchId, input, userId, isAdmin, isManager);
                    if (outputQuantityValidationResult.Status != Const.SUCCESS_READ_CODE)
                    {
                        return outputQuantityValidationResult;
                    }
                }
                
                // Validate waste nếu có
                if (hasWasteData)
                {
                    var wasteValidationResult = await ValidateWasteBeforeAdvanceProgress(batchId, input);
                    if (wasteValidationResult.Status != Const.SUCCESS_READ_CODE)
                    {
                        return wasteValidationResult;
                    }
                }
                
                // 2. Parse parameters từ request
                var parameters = await ParseParametersFromRequest(input);
                
                // 3. Tạo advance progress DTO
                var advanceDto = new AdvanceProcessingBatchProgressDto
                {
                    ProgressDate = input.ProgressDate,
                    OutputQuantity = input.OutputQuantity,
                    OutputUnit = input.OutputUnit,
                    PhotoUrl = null,
                    VideoUrl = null,
                    Parameters = parameters.Any() ? parameters : null,
                    StageId = input.StageId,
                    CurrentStageId = input.CurrentStageId,
                    StageDescription = input.StageDescription
                };

                // 4. Advance progress
                var advanceResult = await AdvanceProgressByBatchIdAsync(batchId, advanceDto, userId, isAdmin, isManager);
                if (advanceResult.Status != Const.SUCCESS_CREATE_CODE && advanceResult.Status != Const.SUCCESS_UPDATE_CODE)
                {
                    return advanceResult;
                }

                // 5. Lấy progressId mới nhất
                var latestProgressResult = await GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);
                var actualProgressId = Guid.Empty;
                
                if (latestProgressResult.Status == Const.SUCCESS_READ_CODE && latestProgressResult.Data is List<ProcessingBatchProgressViewAllDto> progressesList)
                {
                    var latestProgressDto = progressesList.LastOrDefault();
                    if (latestProgressDto != null)
                    {
                        actualProgressId = latestProgressDto.ProgressId;
                    }
                }

                // 6. Tạo waste nếu có - từ field riêng biệt hoặc từ array
                var createdWastes = new List<ProcessingWasteViewAllDto>();
                Console.WriteLine($"🔍 ADVANCE SERVICE: About to process wastes for progressId: {actualProgressId}");
                
                // Kiểm tra waste từ field riêng biệt trước
                if (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit))
                {
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Creating waste from individual fields - Type: {input.WasteType}, Quantity: {input.WasteQuantity}, Unit: {input.WasteUnit}");
                    var wasteDto = new ProcessingWasteCreateDto
                    {
                        WasteType = input.WasteType,
                        Quantity = input.WasteQuantity.Value,
                        Unit = input.WasteUnit,
                        Note = input.WasteNote,
                        RecordedAt = input.WasteRecordedAt ?? DateTime.UtcNow
                    };
                    var wasteList = new List<ProcessingWasteCreateDto> { wasteDto };
                    createdWastes = await CreateWastesForProgress(wasteList, actualProgressId, userId, isAdmin);
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Created waste from individual fields, count: {createdWastes.Count}");
                }
                // Nếu không có field riêng biệt, kiểm tra array
                else if (input.Wastes?.Any() == true)
                {
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Creating wastes from array, count: {input.Wastes.Count}");
                    createdWastes = await CreateWastesForProgress(input.Wastes, actualProgressId, userId, isAdmin);
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Created wastes from array, count: {createdWastes.Count}");
                }
                else
                {
                    Console.WriteLine($"🔍 ADVANCE SERVICE: No valid waste data found to process");
                }

                // 7. Tạo response parameters từ input (tránh query lại DB gây conflict)
                var responseParameters = new List<ProcessingParameterViewAllDto>();
                if (parameters.Any())
                {
                    responseParameters = parameters.Select(p => new ProcessingParameterViewAllDto
                    {
                        ParameterId = Guid.NewGuid(), // Tạm thời, sẽ được cập nhật khi lấy từ DB
                        ProgressId = actualProgressId,
                        ParameterName = p.ParameterName,
                        ParameterValue = p.ParameterValue,
                        Unit = p.Unit,
                        RecordedAt = p.RecordedAt
                    }).ToList();
                }

                // 7. Tạo response DTO
                Console.WriteLine($"🔍 ADVANCE SERVICE: Creating response with {createdWastes.Count} wastes");
                var response = new ProcessingBatchProgressMediaResponse
                {
                    Message = advanceResult.Message,
                    ProgressId = actualProgressId,
                    PhotoUrl = null,
                    VideoUrl = null,
                    MediaCount = 0,
                    AllPhotoUrls = new List<string>(),
                    AllVideoUrls = new List<string>(),
                    Parameters = responseParameters,
                    Wastes = createdWastes
                };

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, response);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi advance progress với waste: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse parameters từ advance request
        /// </summary>
        private async Task<List<ProcessingParameterInProgressDto>> ParseParametersFromRequest(AdvanceProcessingBatchProgressRequest request)
        {
            var parameters = new List<ProcessingParameterInProgressDto>();
            
            // Single parameter
            if (!string.IsNullOrEmpty(request.ParameterName))
            {
                parameters.Add(new ProcessingParameterInProgressDto
                {
                    ParameterName = request.ParameterName,
                    ParameterValue = request.ParameterValue,
                    Unit = request.Unit,
                    RecordedAt = request.RecordedAt
                });
            }
            
            // Multiple parameters từ JSON array
            if (!string.IsNullOrEmpty(request.ParametersJson))
            {
                try
                {
                    var multipleParams = System.Text.Json.JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(request.ParametersJson);
                    if (multipleParams != null)
                    {
                        parameters.AddRange(multipleParams);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing parameters JSON: {ex.Message}");
                }
            }
            
            return parameters;
        }

        /// <summary>
        /// 🔧 VALIDATION: Kiểm tra waste trước khi tạo progress (pre-validation)
        /// </summary>
        private async Task<IServiceResult> ValidateWasteBeforeCreateProgress(Guid batchId, ProcessingBatchProgressCreateRequest input)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Starting pre-validation for batchId: {batchId}");
                
                // 1. Kiểm tra nếu có waste data
                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit)) ||
                                   (input.Wastes?.Any() == true);
                
                if (!hasWasteData)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: No waste data found, skipping validation");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                // 2. Lấy batch
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                // 3. Kiểm tra khối lượng đầu ra của progress hiện tại (từ input)
                if (!input.OutputQuantity.HasValue || input.OutputQuantity.Value <= 0)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Current input has no valid output quantity");
                    return CreateMissingInfoError("OutputQuantity", "OutputQuantity");
                }

                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit ?? "kg";

                // 🔧 VALIDATION: Kiểm tra waste dựa trên khối lượng vào batch và khối lượng ra progress
                // Waste phải <= (InputQuantity - OutputQuantity)
                var maxAllowedWasteFromBatch = batch.InputQuantity - currentOutputQuantity;
                
                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Batch-based waste validation:");
                Console.WriteLine($"  - Batch input quantity: {batch.InputQuantity} {batch.InputUnit}");
                Console.WriteLine($"  - Current output quantity: {currentOutputQuantity} {currentOutputUnit}");
                Console.WriteLine($"  - Max allowed waste from batch: {maxAllowedWasteFromBatch} {batch.InputUnit}");

                // 4. Tính tổng khối lượng waste từ input
                double totalWasteQuantity = 0;
                
                // Từ field riêng biệt
                if (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit))
                {
                    var wasteQuantityInKg = ConvertToKg(input.WasteQuantity.Value, input.WasteUnit);
                    totalWasteQuantity += wasteQuantityInKg;
                    Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Individual waste: {input.WasteQuantity.Value} {input.WasteUnit} = {wasteQuantityInKg} kg");
                }
                
                // Từ array
                if (input.Wastes?.Any() == true)
                {
                    foreach (var wasteDto in input.Wastes)
                    {
                        var wasteQuantityInKg = ConvertToKg(wasteDto.Quantity, wasteDto.Unit);
                        totalWasteQuantity += wasteQuantityInKg;
                        Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Array waste {wasteDto.WasteType}: {wasteDto.Quantity} {wasteDto.Unit} = {wasteQuantityInKg} kg");
                    }
                }

                // 5. Chuyển đổi về cùng đơn vị để so sánh
                var batchInputQuantityInKg = ConvertToKg(batch.InputQuantity, batch.InputUnit);
                var currentQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);
                var maxAllowedWasteInKg = batchInputQuantityInKg - currentQuantityInKg;

                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Total waste: {totalWasteQuantity} kg, Max allowed from batch: {maxAllowedWasteInKg} kg");

                // 6. Validation cuối cùng - Waste phải <= (InputQuantity - OutputQuantity)
                if (totalWasteQuantity > maxAllowedWasteInKg)
                {
                    // 🔧 FIX: Xử lý trường hợp maxAllowedWasteInKg = 0
                    double maxAllowedWithTolerance;
                    if (maxAllowedWasteInKg <= 0)
                    {
                        // Nếu không được phép waste (maxAllowedWasteInKg = 0), chỉ cho phép tolerance nhỏ
                        maxAllowedWithTolerance = 1.0; // Cho phép tối đa 1kg khi không được phép waste
                    }
                    else
                    {
                        var tolerance = 0.10; // Cho phép sai số 10% hoặc tối đa 5kg
                        maxAllowedWithTolerance = Math.Max(maxAllowedWasteInKg * (1 + tolerance), maxAllowedWasteInKg + 5.0);
                    }
                    
                    if (totalWasteQuantity > maxAllowedWithTolerance)
                    {
                        return CreateWasteQuantityExceedsBatchLimitError(totalWasteQuantity, maxAllowedWasteInKg, 
                            batch.InputQuantity, batch.InputUnit, currentOutputQuantity, currentOutputUnit);
                    }
                    else
                    {
                        Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Warning - waste quantity exceeds limit but within tolerance");
                    }
                }

                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Pre-validation passed successfully");
                                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Error during pre-validation: {ex.Message}");
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi pre-validate waste: {ex.Message}");
            }
        }



        /// <summary>
        /// 🔧 HELPER: Chuyển đổi đơn vị về kg để so sánh
        /// </summary>
        private double ConvertToKg(double quantity, string unit)
        {
            var unitLower = unit?.Trim().ToLower() ?? "kg";
            
            return unitLower switch
            {
                "kg" => quantity,
                "g" => quantity / 1000.0,
                "tấn" => quantity * 1000.0,
                "ton" => quantity * 1000.0,
                "lít" => quantity, // Giả sử 1 lít = 1 kg cho cà phê
                "ml" => quantity / 1000.0,
                "bao" => quantity * 50.0, // Giả sử 1 bao = 50 kg
                "thùng" => quantity * 25.0, // Giả sử 1 thùng = 25 kg
                _ => quantity // Mặc định giữ nguyên
            };
        }



        /// <summary>
        /// 🔧 HELPER: Tạo lỗi waste quantity validation
        /// </summary>
        private IServiceResult CreateWasteQuantityError(double totalWaste, double maxAllowed, 
            double previousOutput, string previousUnit, double currentOutput, string currentUnit)
        {
            var parameters = new Dictionary<string, object>
            {
                ["TotalWaste"] = totalWaste,
                ["MaxAllowed"] = maxAllowed,
                ["PreviousOutput"] = previousOutput,
                ["PreviousUnit"] = previousUnit,
                ["CurrentOutput"] = currentOutput,
                ["CurrentUnit"] = currentUnit,
                ["WasteExceeded"] = totalWaste - maxAllowed
            };

            return CreateValidationError("WasteQuantityExceeded", parameters);
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi waste quantity vượt quá giới hạn batch
        /// </summary>
        private IServiceResult CreateWasteQuantityExceedsBatchLimitError(double totalWaste, double maxAllowed, 
            double batchInput, string batchInputUnit, double currentOutput, string currentOutputUnit)
        {
            var parameters = new Dictionary<string, object>
            {
                ["TotalWaste"] = totalWaste,
                ["MaxAllowed"] = maxAllowed,
                ["BatchInputQuantity"] = batchInput,
                ["BatchInputUnit"] = batchInputUnit,
                ["CurrentOutput"] = currentOutput,
                ["CurrentOutputUnit"] = currentOutputUnit,
                ["WasteExceeded"] = totalWaste - maxAllowed
            };

            return CreateFieldValidationError("WasteQuantityExceedsBatchLimit", "WasteQuantity", parameters);
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi logic khối lượng
        /// </summary>
        private IServiceResult CreateLogicQuantityError(double previousOutput, string previousUnit, 
            double currentOutput, string currentUnit)
        {
            var parameters = new Dictionary<string, object>
            {
                ["PreviousOutput"] = previousOutput,
                ["PreviousUnit"] = previousUnit,
                ["CurrentOutput"] = currentOutput,
                ["CurrentUnit"] = currentUnit,
                ["Difference"] = currentOutput - previousOutput
            };

            return CreateValidationError("InvalidOutputQuantityLogic", parameters);
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi thiếu thông tin
        /// </summary>
        private IServiceResult CreateMissingInfoError(string fieldName, string fieldType = "OutputQuantity")
        {
            var parameters = new Dictionary<string, object>
            {
                ["FieldName"] = fieldName,
                ["FieldType"] = fieldType,
                ["Required"] = true
            };

            return CreateFieldValidationError("MissingRequiredField", fieldName, parameters);
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi không tìm thấy dữ liệu
        /// </summary>
        private IServiceResult CreateNotFoundError(string entityName, string entityId = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["EntityId"] = entityId
            };

            return CreateValidationError("EntityNotFound", parameters);
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi không có quyền
        /// </summary>
        private IServiceResult CreatePermissionError(string action, string resource, string userId = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Action"] = action,
                ["Resource"] = resource,
                ["UserId"] = userId
            };

            return CreateValidationError("PermissionDenied", parameters);
        }

        /// <summary>
        /// 🔧 HELPER: Tạo lỗi trạng thái không hợp lệ
        /// </summary>
        private IServiceResult CreateInvalidStatusError(string currentStatus, string expectedStatus, string entityName = "Batch")
        {
            var parameters = new Dictionary<string, object>
            {
                ["CurrentStatus"] = currentStatus,
                ["ExpectedStatus"] = expectedStatus,
                ["EntityName"] = entityName
            };

            return CreateValidationError("InvalidStatus", parameters);
        }



        /// <summary>
        /// 🔧 VALIDATION: Kiểm tra waste trước khi advance progress
        /// </summary>
        private async Task<IServiceResult> ValidateWasteBeforeAdvanceProgress(Guid batchId, AdvanceProcessingBatchProgressRequest input)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Starting pre-validation for batchId: {batchId}");
                
                // 1. Kiểm tra nếu có waste data
                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit)) ||
                                   (input.Wastes?.Any() == true);
                
                if (!hasWasteData)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: No waste data found, skipping validation");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                // 2. Lấy batch và progress hiện tại
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                // 3. Lấy progress hiện tại (nếu có)
                var currentProgresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex)
                );

                var currentProgress = currentProgresses.FirstOrDefault();
                if (currentProgress == null)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: No current progress found, this is first step");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                // 4. Lấy progress trước đó
                var previousProgresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && p.StepIndex < currentProgress.StepIndex && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex)
                );

                var previousProgress = previousProgresses.FirstOrDefault();
                if (previousProgress == null)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: No previous progress found");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                // 5. Kiểm tra khối lượng đầu ra của bước trước
                if (!previousProgress.OutputQuantity.HasValue || previousProgress.OutputQuantity.Value <= 0)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Previous progress has no valid output quantity");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                var previousOutputQuantity = previousProgress.OutputQuantity.Value;
                var previousOutputUnit = previousProgress.OutputUnit ?? "kg";

                // 6. Kiểm tra khối lượng đầu ra của bước hiện tại (từ input)
                if (!input.OutputQuantity.HasValue || input.OutputQuantity.Value <= 0)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Current input has no valid output quantity");
                    return CreateMissingInfoError("OutputQuantity", "OutputQuantity");
                }

                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit ?? "kg";

                // 🔧 VALIDATION MỚI: Kiểm tra waste dựa trên progress trước đó
                // Waste phải <= (PreviousOutputQuantity - CurrentOutputQuantity)
                var maxAllowedWasteFromPrevious = previousOutputQuantity - currentOutputQuantity;
                
                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Previous progress-based waste validation:");
                Console.WriteLine($"  - Previous output quantity: {previousOutputQuantity} {previousOutputUnit}");
                Console.WriteLine($"  - Current output quantity: {currentOutputQuantity} {currentOutputUnit}");
                Console.WriteLine($"  - Max allowed waste from previous: {maxAllowedWasteFromPrevious} {previousOutputUnit}");

                // 7. Kiểm tra nếu khối lượng đầu ra tăng hoặc bằng (không hợp lý)
                if (maxAllowedWasteFromPrevious <= 0)
                {
                    if (maxAllowedWasteFromPrevious < 0)
                    {
                        return CreateValidationError("InvalidOutputQuantityIncrease", new Dictionary<string, object>
                        {
                            ["PreviousOutput"] = previousOutputQuantity,
                            ["PreviousUnit"] = previousOutputUnit,
                            ["CurrentOutput"] = currentOutputQuantity,
                            ["CurrentUnit"] = currentOutputUnit
                        });
                    }
                    else // maxAllowedWasteFromPrevious == 0
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["previousOutput"] = previousOutputQuantity,
                            ["previousUnit"] = previousOutputUnit,
                            ["currentOutput"] = currentOutputQuantity,
                            ["currentUnit"] = currentOutputUnit
                        };
                        return CreateValidationError("InvalidOutputQuantityEqual", parameters);
                    }
                }

                // 8. Tính tổng khối lượng waste từ input
                double totalWasteQuantity = 0;
                
                // Từ field riêng biệt
                if (!string.IsNullOrEmpty(input.WasteType) && input.WasteQuantity > 0 && !string.IsNullOrEmpty(input.WasteUnit))
                {
                    var wasteQuantityInKg = ConvertToKg(input.WasteQuantity.Value, input.WasteUnit);
                    totalWasteQuantity += wasteQuantityInKg;
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Individual waste: {input.WasteQuantity.Value} {input.WasteUnit} = {wasteQuantityInKg} kg");
                }
                
                // Từ array
                if (input.Wastes?.Any() == true)
                {
                    foreach (var wasteDto in input.Wastes)
                    {
                        var wasteQuantityInKg = ConvertToKg(wasteDto.Quantity, wasteDto.Unit);
                        totalWasteQuantity += wasteQuantityInKg;
                        Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Array waste {wasteDto.WasteType}: {wasteDto.Quantity} {wasteDto.Unit} = {wasteQuantityInKg} kg");
                    }
                }

                // 9. Chuyển đổi về cùng đơn vị để so sánh
                var previousQuantityInKg = ConvertToKg(previousOutputQuantity, previousOutputUnit);
                var currentQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);
                var maxAllowedWasteInKg = previousQuantityInKg - currentQuantityInKg;

                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Total waste: {totalWasteQuantity} kg, Max allowed from previous: {maxAllowedWasteInKg} kg");

                // 10b. Validation cuối cùng - Waste phải <= (PreviousOutputQuantity - CurrentOutputQuantity)
                if (totalWasteQuantity > maxAllowedWasteInKg)
                {
                    // 🔧 FIX: Xử lý trường hợp maxAllowedWasteInKg = 0
                    double maxAllowedWithTolerance;
                    if (maxAllowedWasteInKg <= 0)
                    {
                        // Nếu không được phép waste (maxAllowedWasteInKg = 0), chỉ cho phép tolerance nhỏ
                        maxAllowedWithTolerance = 1.0; // Cho phép tối đa 1kg khi không được phép waste
                    }
                    else
                    {
                        var tolerance = 0.10; // Cho phép sai số 10% hoặc tối đa 5kg
                        maxAllowedWithTolerance = Math.Max(maxAllowedWasteInKg * (1 + tolerance), maxAllowedWasteInKg + 5.0);
                    }
                    
                    if (totalWasteQuantity > maxAllowedWithTolerance)
                    {
                        return CreateWasteQuantityError(totalWasteQuantity, maxAllowedWasteInKg, 
                            previousOutputQuantity, previousOutputUnit, currentOutputQuantity, currentOutputUnit);
                    }
                    else
                    {
                        Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Warning - waste quantity exceeds limit but within tolerance");
                    }
                }

                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Pre-validation passed successfully");
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Error during pre-validation: {ex.Message}");
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi pre-validate waste: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔧 VALIDATION: Kiểm tra ngày progress hợp lệ
        /// </summary>
        private async Task<IServiceResult> ValidateProgressDate(Guid batchId, DateOnly? progressDate, List<ProcessingBatchProgress> existingProgresses)
        {
            try
            {
                // 1. Kiểm tra ngày progress có tồn tại
                if (!progressDate.HasValue)
                {
                    return CreateFieldValidationError("ProgressDate", "ProgressDate");
                }

                var selectedDate = progressDate.Value;
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                // 2. Không cho phép ngày trong tương lai
                if (selectedDate > today)
                {
                    return CreateValidationError("ProgressDateInFuture", new Dictionary<string, object>
                    {
                        ["ProgressDate"] = selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                        ["Today"] = today.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                    });
                }

                // 3. Không cho phép ngày quá xa trong quá khứ (tối đa 1 năm)
                var minDatePast = today.AddDays(-365);
                if (selectedDate < minDatePast)
                {
                    return CreateValidationError("ProgressDateTooPast", new Dictionary<string, object>
                    {
                        ["ProgressDate"] = selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                        ["MinDate"] = minDatePast.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                    });
                }

                // 4. Lấy thông tin batch để kiểm tra ngày thu hoạch
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                // 5. Nếu là progress đầu tiên (chưa có progress nào)
                if (!existingProgresses.Any())
                {
                    // Lấy bước cuối cùng từ crop progress (không chỉ harvesting)
                    var allCropProgress = await _unitOfWork.CropProgressRepository.GetAllAsync(
                        p => p.CropSeasonDetail.CropSeasonId == batch.CropSeasonId && 
                             p.CropSeasonDetail.CommitmentDetail.PlanDetail.CoffeeTypeId == batch.CoffeeTypeId && 
                             p.ProgressDate.HasValue && 
                             !p.IsDeleted && 
                             !p.CropSeasonDetail.IsDeleted,
                        include: q => q.Include(p => p.CropSeasonDetail)
                                      .ThenInclude(d => d.CommitmentDetail)
                                      .ThenInclude(cd => cd.PlanDetail)
                                      .Include(p => p.Stage)
                    );

                    if (allCropProgress.Any())
                    {
                        // Lấy ngày của bước cuối cùng (ngày mới nhất)
                        var lastProgressDate = allCropProgress.Max(p => p.ProgressDate.Value);
                        
                        // Debug log để kiểm tra
                        Console.WriteLine($"🔍 DEBUG FirstProgressDateAfterHarvest:");
                        Console.WriteLine($"  - Selected Date: {selectedDate} ({selectedDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                        Console.WriteLine($"  - Last Progress Date: {lastProgressDate} ({lastProgressDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                        Console.WriteLine($"  - Comparison: {selectedDate} < {lastProgressDate} = {selectedDate < lastProgressDate}");
                        
                        // Progress đầu tiên phải từ ngày bước cuối cùng trở đi (cho phép cùng ngày)
                        if (selectedDate < lastProgressDate)
                        {
                            return CreateValidationError("FirstProgressDateAfterHarvest", new Dictionary<string, object>
                            {
                                ["ProgressDate"] = selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                ["HarvestDate"] = lastProgressDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                ["MinDate"] = lastProgressDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                            });
                        }
                    }
                    else
                    {
                        // Nếu không có crop progress thu hoạch, fallback về crop season detail
                        var cropSeasonDetail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                            d => d.CropSeasonId == batch.CropSeasonId && 
                                 d.CommitmentDetail.PlanDetail.CoffeeTypeId == batch.CoffeeTypeId && 
                                 !d.IsDeleted,
                            include: q => q.Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail)
                        );

                        if (cropSeasonDetail?.ExpectedHarvestEnd.HasValue == true)
                        {
                            var harvestEndDate = cropSeasonDetail.ExpectedHarvestEnd.Value;
                            
                            // Debug log để kiểm tra fallback
                            Console.WriteLine($"🔍 DEBUG FirstProgressDateAfterHarvest (Fallback):");
                            Console.WriteLine($"  - Selected Date: {selectedDate} ({selectedDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                            Console.WriteLine($"  - Expected Harvest End: {harvestEndDate} ({harvestEndDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                            Console.WriteLine($"  - Comparison: {selectedDate} < {harvestEndDate} = {selectedDate < harvestEndDate}");
                            
                            // Progress đầu tiên phải từ ngày thu hoạch trở đi (cho phép cùng ngày thu hoạch)
                            if (selectedDate < harvestEndDate)
                            {
                                return CreateValidationError("FirstProgressDateAfterHarvest", new Dictionary<string, object>
                                {
                                    ["ProgressDate"] = selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                    ["HarvestDate"] = harvestEndDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                    ["MinDate"] = harvestEndDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                                });
                            }
                        }
                    }
                }
                else
                {
                    // 6. Nếu không phải progress đầu tiên, kiểm tra với progress trước đó
                    var latestProgress = existingProgresses.OrderByDescending(p => p.StepIndex).First();
                    
                    // Progress mới phải từ ngày của progress trước đó trở đi (cho phép cùng ngày)
                    if (selectedDate < latestProgress.ProgressDate.Value)
                    {
                        return CreateValidationError("ProgressDateAfterPrevious", new Dictionary<string, object>
                        {
                            ["ProgressDate"] = selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                            ["PreviousProgressDate"] = latestProgress.ProgressDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                            ["MinDate"] = latestProgress.ProgressDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                        });
                    }
                }

                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi validate ngày progress: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔧 VALIDATION: Kiểm tra khối lượng đầu ra trước khi tạo progress
        /// </summary>
        private async Task<IServiceResult> ValidateOutputQuantityBeforeCreateProgress(Guid batchId, ProcessingBatchProgressCreateRequest input)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Starting validation for batchId: {batchId}");
                
                // 1. Kiểm tra khối lượng đầu ra có tồn tại và hợp lệ
                if (!input.OutputQuantity.HasValue || input.OutputQuantity.Value <= 0)
                {
                    Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Invalid output quantity: {input.OutputQuantity}");
                    return CreateFieldValidationError("OutputQuantity", "OutputQuantity");
                }

                // 2. Lấy batch để kiểm tra
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit ?? "kg";
                var batchInputQuantity = batch.InputQuantity;
                var batchInputUnit = batch.InputUnit;

                // Chuyển đổi về cùng đơn vị để so sánh
                var batchInputQuantityInKg = ConvertToKg(batchInputQuantity, batchInputUnit);
                var currentOutputQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);

                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Quantity validation:");
                Console.WriteLine($"  - Batch input: {batchInputQuantity} {batchInputUnit} = {batchInputQuantityInKg} kg");
                Console.WriteLine($"  - Current output: {currentOutputQuantity} {currentOutputUnit} = {currentOutputQuantityInKg} kg");

                // 3. 🔧 VALIDATION: Kiểm tra logic khối lượng (giống advance)
                // 3a. Khối lượng đầu ra không được vượt quá khối lượng đầu vào
                if (currentOutputQuantityInKg > batchInputQuantityInKg)
                {
                    return CreateFieldValidationError("OutputQuantityExceedsInput", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["InputQuantity"] = batchInputQuantity,
                        ["InputUnit"] = batchInputUnit,
                        ["OutputQuantity"] = currentOutputQuantity,
                        ["OutputUnit"] = currentOutputUnit
                    });
                }

                // 3b. 🔧 NEW: Không cho phép output quantity bằng với input (luôn phải giảm sau sơ chế)
                if (Math.Abs(currentOutputQuantityInKg - batchInputQuantityInKg) < 0.01) // Bằng nhau (tolerance 0.01 kg)
                {
                    return CreateFieldValidationError("OutputQuantityEqualNotAllowed", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["PreviousOutputQuantity"] = batchInputQuantity,
                        ["PreviousOutputUnit"] = batchInputUnit,
                        ["CurrentOutputQuantity"] = currentOutputQuantity,
                        ["CurrentOutputUnit"] = currentOutputUnit
                    });
                }

                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Validation passed successfully");
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Error during validation: {ex.Message}");
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi validate output quantity: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔧 VALIDATION: Kiểm tra khối lượng đầu ra trước khi advance progress
        /// </summary>
        private async Task<IServiceResult> ValidateOutputQuantityBeforeAdvanceProgress(Guid batchId, AdvanceProcessingBatchProgressRequest input, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Starting validation for batchId: {batchId}");
                
                // 1. Kiểm tra khối lượng đầu ra có tồn tại và hợp lệ
                if (!input.OutputQuantity.HasValue || input.OutputQuantity.Value <= 0)
                {
                    Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Invalid output quantity: {input.OutputQuantity}");
                    return CreateFieldValidationError("OutputQuantity", "OutputQuantity");
                }

                // 2. Lấy batch để kiểm tra
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                // 3. Lấy progress cuối cùng để so sánh
                var latestProgressResult = await GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);
                if (latestProgressResult.Status != Const.SUCCESS_READ_CODE || latestProgressResult.Data is not List<ProcessingBatchProgressViewAllDto> progressesList || !progressesList.Any())
                {
                    return CreateValidationError("NoPreviousProgress", new Dictionary<string, object>
                    {
                        ["BatchId"] = batchId.ToString()
                    });
                }

                var latestProgress = progressesList.Last();
                var previousOutputQuantity = latestProgress.OutputQuantity ?? 0;
                var previousOutputUnit = latestProgress.OutputUnit ?? "kg";
                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit ?? "kg";

                // Chuyển đổi về cùng đơn vị để so sánh
                var previousOutputQuantityInKg = ConvertToKg(previousOutputQuantity, previousOutputUnit);
                var currentOutputQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);

                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Quantity validation:");
                Console.WriteLine($"  - Previous output: {previousOutputQuantity} {previousOutputUnit} = {previousOutputQuantityInKg} kg");
                Console.WriteLine($"  - Current output: {currentOutputQuantity} {currentOutputUnit} = {currentOutputQuantityInKg} kg");

                // 4. 🔧 VALIDATION: Kiểm tra logic khối lượng giữa các step
                // 4a. Khối lượng đầu ra mới không được vượt quá khối lượng đầu ra trước đó
                if (currentOutputQuantityInKg > previousOutputQuantityInKg)
                {
                    return CreateFieldValidationError("OutputQuantityExceedsPrevious", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["PreviousOutputQuantity"] = previousOutputQuantity,
                        ["PreviousOutputUnit"] = previousOutputUnit,
                        ["CurrentOutputQuantity"] = currentOutputQuantity,
                        ["CurrentOutputUnit"] = currentOutputUnit
                    });
                }

                // 4b. 🔧 NEW: Không cho phép output quantity bằng nhau (luôn phải giảm sau sơ chế)
                if (Math.Abs(currentOutputQuantityInKg - previousOutputQuantityInKg) < 0.01) // Bằng nhau (tolerance 0.01 kg)
                {
                    return CreateFieldValidationError("OutputQuantityEqualNotAllowed", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["PreviousOutputQuantity"] = previousOutputQuantity,
                        ["PreviousOutputUnit"] = previousOutputUnit,
                        ["CurrentOutputQuantity"] = currentOutputQuantity,
                        ["CurrentOutputUnit"] = currentOutputUnit
                    });
                }

                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Validation passed successfully");
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Error during validation: {ex.Message}");
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi validate output quantity: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo validation error với field name để frontend hiển thị dưới field
        /// </summary>
        private ServiceResult CreateFieldValidationError(string errorKey, string fieldName, Dictionary<string, object> parameters = null)
        {
            var errorData = new
            {
                ErrorKey = errorKey,
                FieldName = fieldName, // Thêm field name để frontend biết field nào bị lỗi
                Parameters = parameters ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                ErrorType = "FieldValidationError"
            };

            // Tạo message rõ ràng hơn
            string message = GetFieldValidationErrorMessage(errorKey, fieldName, parameters);
            
            return new ServiceResult(Const.ERROR_VALIDATION_CODE, message, errorData);
        }

        /// <summary>
        /// Tạo validation error message rõ ràng cho field
        /// </summary>
        private string GetFieldValidationErrorMessage(string errorKey, string fieldName, Dictionary<string, object> parameters)
        {
            // 🔧 Sử dụng error key để frontend có thể translate
            return errorKey;
        }
    }
}
