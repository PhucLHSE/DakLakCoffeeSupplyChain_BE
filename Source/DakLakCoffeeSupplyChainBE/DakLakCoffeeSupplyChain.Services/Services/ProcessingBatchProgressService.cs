using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
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
            _unitOfWork=unitOfWork;
            _codeGenerator=codeGenerator;
            _evaluationService=evaluationService;
        }

        private ServiceResult CreateValidationError(string errorKey, Dictionary<string, object> parameters = null)
        {
            var errorData = new
            {
                ErrorKey = errorKey,
                Parameters = parameters??new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                ErrorType = "ValidationError"
            };

            return new ServiceResult(Const.ERROR_VALIDATION_CODE, errorKey, errorData);
        }

        public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin, bool isManager)
        {
            var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                predicate: x => !x.IsDeleted,
                include: q => q
                    .Include(x => x.Stage)
                    .Include(x => x.UpdatedByNavigation).ThenInclude(u => u.User),
                asNoTracking: true
            );

            if (progresses==null||!progresses.Any())
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<ProcessingBatchProgressViewAllDto>());
            }

            var batchIds = progresses.Select(p => p.BatchId).Distinct().ToList();

            var batches = await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                predicate: x => batchIds.Contains(x.BatchId)&&!x.IsDeleted,
                include: q => q
                    .Include(b => b.CropSeason).ThenInclude(cs => cs.Commitment)
                    .Include(b => b.Farmer),
                asNoTracking: true
            );

            var batchDict = batches.ToDictionary(b => b.BatchId, b => b);

            if (!isAdmin)
            {
                if (isManager)
                {
                    var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(m => m.UserId==userId&&!m.IsDeleted);
                    if (manager==null)
                    {
                        return CreateValidationError("BusinessManagerNotFound");
                    }

                    var managerId = manager.ManagerId;

                    progresses=progresses
                        .Where(p => batchDict.ContainsKey(p.BatchId)&&
                                    batchDict[p.BatchId].CropSeason?.Commitment?.ApprovedBy==managerId)
                        .ToList();

                    if (!progresses.Any())
                    {
                        return CreateValidationError("NoPermissionToAccessAnyProgress");
                    }
                }
                else
                {
                    var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId==userId&&!f.IsDeleted);
                    if (farmer==null)
                        return CreateValidationError("FarmerNotFound");

                    progresses=progresses
                        .Where(p => batchDict.ContainsKey(p.BatchId)&&
                                    batchDict[p.BatchId].FarmerId==farmer.FarmerId)
                        .ToList();
                }
            }

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

            if (entity==null)
                return CreateValidationError("ProgressNotFound", new Dictionary<string, object>
                {
                    ["ProgressId"]=progressId.ToString()
                });

            var mediaFiles = await _unitOfWork.MediaFileRepository.GetAllAsync(
                m => !m.IsDeleted&&
                     m.RelatedEntity=="ProcessingProgress"&&
                     m.RelatedId==progressId,
                orderBy: q => q.OrderByDescending(m => m.UploadedAt)
            );

            var mediaDtos = mediaFiles.Select(m => new MediaFileResponse
            {
                MediaId=m.MediaId,
                MediaUrl=m.MediaUrl,
                MediaType=m.MediaType,
                Caption=m.Caption,
                UploadedAt=m.UploadedAt
            }).ToList();

            var dto = entity.MapToProcessingBatchProgressDetailDto();

            var photoFiles = mediaFiles.Where(m => m.MediaType=="image").ToList();
            var videoFiles = mediaFiles.Where(m => m.MediaType=="video").ToList();

            dto.PhotoUrl=photoFiles.Any() ? photoFiles.First().MediaUrl : null;
            dto.VideoUrl=videoFiles.Any() ? videoFiles.First().MediaUrl : null;

            dto.MediaFiles=mediaDtos;

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }

        public async Task<IServiceResult> GetAllByBatchIdAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: b => b.BatchId==batchId&&!b.IsDeleted,
                    include: q => q
                        .Include(b => b.CropSeason).ThenInclude(cs => cs.Commitment)
                        .Include(b => b.Farmer),
                    asNoTracking: true
                );

                if (batch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (!isAdmin)
                {
                    if (isManager)
                    {
                        var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                            m => m.UserId==userId&&!m.IsDeleted,
                            asNoTracking: true
                        );

                        if (manager==null)
                        {
                            return CreateValidationError("BusinessManagerNotFound", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString()
                            });
                        }

                        if (batch.CropSeason?.Commitment?.ApprovedBy!=manager.ManagerId)
                        {
                            return CreateValidationError("NoPermissionToAccessBatch", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString(),
                                ["BatchId"]=batchId.ToString()
                            });
                        }
                    }
                    else
                    {
                        var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                            f => f.UserId==userId&&!f.IsDeleted,
                            asNoTracking: true
                        );

                        if (farmer==null)
                        {
                            return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString()
                            });
                        }

                        if (batch.FarmerId!=farmer.FarmerId)
                        {
                            return CreateValidationError("NoPermissionToAccessBatch", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString(),
                                ["BatchId"]=batchId.ToString()
                            });
                        }
                    }
                }

                var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    predicate: p => p.BatchId==batchId&&!p.IsDeleted,
                    include: q => q
                        .Include(p => p.Stage)
                        .Include(p => p.UpdatedByNavigation).ThenInclude(u => u.User)
                        .Include(p => p.ProcessingParameters.Where(pp => !pp.IsDeleted)),
                    orderBy: q => q.OrderBy(p => p.StepIndex),
                    asNoTracking: true
                );

                foreach (var progress in progresses)
                {
                    Console.WriteLine($"Progress {progress.ProgressId}: {progress.ProcessingParameters?.Count??0} parameters");
                    if (progress.ProcessingParameters?.Any()==true)
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
                        ["BatchId"]=batchId.ToString()
                    });
                }

                var dtoList = new List<ProcessingBatchProgressViewAllDto>();

                foreach (var progress in progresses)
                {
                    var dto = progress.MapToProcessingBatchProgressViewAllDto(batch);

                    var mediaFiles = await _unitOfWork.MediaFileRepository.GetAllAsync(
                        m => !m.IsDeleted&&
                             m.RelatedEntity=="ProcessingProgress"&&
                             m.RelatedId==progress.ProgressId,
                        orderBy: q => q.OrderByDescending(m => m.UploadedAt)
                    );

                    var photoFiles = mediaFiles.Where(m => m.MediaType=="image").ToList();
                    var videoFiles = mediaFiles.Where(m => m.MediaType=="video").ToList();

                    dto.PhotoUrl=photoFiles.Any() ? photoFiles.First().MediaUrl : null;
                    dto.VideoUrl=videoFiles.Any() ? videoFiles.First().MediaUrl : null;

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
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null||batch.IsDeleted)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (!isAdmin&&!isManager)
                {
                    var farmer = (await _unitOfWork.FarmerRepository
                        .GetAllAsync(f => f.UserId==userId&&!f.IsDeleted))
                        .FirstOrDefault();

                    if (farmer==null)
                        return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                        {
                            ["UserId"]=userId.ToString()
                        });

                    if (batch.FarmerId!=farmer.FarmerId)
                        return CreateValidationError("NoPermissionToCreateProgress", new Dictionary<string, object>
                        {
                            ["UserId"]=userId.ToString(),
                            ["BatchId"]=batchId.ToString()
                        });
                }

                var existingProgresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex))).ToList();

                var dateValidationResult = await ValidateProgressDate(batchId, input.ProgressDate, existingProgresses);
                if (dateValidationResult.Status!=Const.SUCCESS_READ_CODE)
                {
                    return dateValidationResult;
                }

                if (input.OutputQuantity.HasValue)
                {
                    if (input.OutputQuantity.Value<=0)
                    {
                        return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                        {
                            ["OutputQuantity"]=input.OutputQuantity.Value,
                            ["MinValue"]=0
                        });
                    }

                    if (input.OutputQuantity.Value>100000)
                    {
                        return CreateValidationError("OutputQuantityTooLarge", new Dictionary<string, object>
                        {
                            ["OutputQuantity"]=input.OutputQuantity.Value,
                            ["MaxValue"]=1000000
                        });
                    }

                    var batchInputUnit = string.IsNullOrWhiteSpace(batch.InputUnit) ? "kg" : batch.InputUnit.Trim().ToLower();
                    var outputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();

                    if (batchInputUnit==outputUnit&&input.OutputQuantity.Value>batch.InputQuantity)
                    {
                        return CreateValidationError("OutputQuantityExceedsInputQuantity", new Dictionary<string, object>
                        {
                            ["OutputQuantity"]=input.OutputQuantity.Value,
                            ["OutputUnit"]=input.OutputUnit??"kg",
                            ["InputQuantity"]=batch.InputQuantity,
                            ["InputUnit"]=batch.InputUnit
                        });
                    }

                    var validUnits = new[] { "kg", "g", "tấn", "lít", "ml", "bao", "thùng", "khác" };
                    var currentOutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();

                    if (!validUnits.Contains(currentOutputUnit))
                    {
                        return CreateValidationError("InvalidOutputUnit", new Dictionary<string, object>
                        {
                            ["InvalidUnit"]=input.OutputUnit,
                            ["ValidUnits"]=string.Join(", ", validUnits)
                        });
                    }

                    if (existingProgresses.Any())
                    {
                        var latestProgress = existingProgresses.Last();
                        if (latestProgress.OutputQuantity.HasValue)
                        {
                            var previousQuantity = latestProgress.OutputQuantity.Value;
                            var currentQuantity = input.OutputQuantity.Value;
                            var changePercentage = ((currentQuantity-previousQuantity)/previousQuantity)*100;

                            Console.WriteLine($"DEBUG CREATE: Quantity comparison:");
                            Console.WriteLine($"  - Previous quantity: {previousQuantity} {latestProgress.OutputUnit}");
                            Console.WriteLine($"  - Current quantity: {currentQuantity} {input.OutputUnit??batch.InputUnit}");
                            Console.WriteLine($"  - Change: {changePercentage:F2}%");

                            const double tolerance = 0.1;

                            if (currentQuantity>previousQuantity*(1+tolerance))
                            {
                                return CreateValidationError("OutputQuantityIncreaseTooHigh", new Dictionary<string, object>
                                {
                                    ["CurrentQuantity"]=currentQuantity,
                                    ["CurrentUnit"]=input.OutputUnit??batch.InputUnit,
                                    ["PreviousQuantity"]=previousQuantity,
                                    ["PreviousUnit"]=latestProgress.OutputUnit,
                                    ["Tolerance"]=tolerance*100,
                                    ["IncreasePercentage"]=changePercentage
                                });
                            }

                            if (changePercentage<-70)
                            {
                                return CreateValidationError("OutputQuantityDecreaseTooHigh", new Dictionary<string, object>
                                {
                                    ["CurrentQuantity"]=currentQuantity,
                                    ["CurrentUnit"]=input.OutputUnit??batch.InputUnit,
                                    ["PreviousQuantity"]=previousQuantity,
                                    ["PreviousUnit"]=latestProgress.OutputUnit,
                                    ["DecreasePercentage"]=Math.Abs(changePercentage)
                                });
                            }
                        }
                    }
                }

                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (!stages.Any())
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"]=batch.MethodId.ToString()
                    });

                var failureInfo = await GetFailureInfoForBatch(batchId);
                if (failureInfo!=null)
                {
                    Console.WriteLine($"DEBUG CREATE: Found failure info for batch - BatchId: {batchId}");
                }
                else
                {
                    Console.WriteLine($"DEBUG CREATE: No failure info found");
                }

                int nextStepIndex = 0;
                int nextStageId = 0;
                bool isLastStep = false;
                bool isRetryScenario = false;

                if (!existingProgresses.Any())
                {
                    nextStageId=stages[0].StageId;
                    nextStepIndex=1;
                    isLastStep=(stages.Count==1);
                }
                else
                {
                    var latestProgress = existingProgresses.Last();
                    var currentStageIndex = stages.FindIndex(s => s.StageId==latestProgress.StageId);

                    if (currentStageIndex==-1)
                        return CreateValidationError("CurrentStageNotFound", new Dictionary<string, object>
                        {
                            ["StageId"]=latestProgress.StageId.ToString()
                        });

                    if (failureInfo!=null)
                    {
                        var failureInfoObj = failureInfo as dynamic;
                        if (failureInfoObj?.FailedStageId!=null&&latestProgress.StageId==failureInfoObj.FailedStageId)
                        {
                            nextStageId=latestProgress.StageId;
                            nextStepIndex=latestProgress.StepIndex+1;
                            isLastStep=(currentStageIndex==stages.Count-1);
                            isRetryScenario=true;

                            Console.WriteLine($"DEBUG: Retry scenario - StageId: {nextStageId}, StepIndex: {nextStepIndex}, isLastStep: {isLastStep}");
                        }
                    }

                    if (!isRetryScenario)
                    {
                        if (currentStageIndex>=stages.Count-1)
                        {
                            return CreateValidationError("CannotCreateNextStepLastStageCompleted", new Dictionary<string, object>
                            {
                                ["StageName"]=stages[currentStageIndex].StageName
                            });
                        }
                        else
                        {
                            var nextStage = stages[currentStageIndex+1];
                            nextStageId=nextStage.StageId;
                            nextStepIndex=latestProgress.StepIndex+1;
                            isLastStep=(nextStage.OrderIndex>=stages.Max(s => s.OrderIndex));

                            Console.WriteLine($"DEBUG: Normal next step - CurrentStageIndex: {currentStageIndex}, StagesCount: {stages.Count}, isLastStep: {isLastStep}");
                            Console.WriteLine($"DEBUG: NextStageId: {nextStageId}, NextStepIndex: {nextStepIndex}");
                        }
                    }
                }

                if (nextStageId==0)
                {
                    Console.WriteLine($"ERROR: nextStageId is not assigned! This should not happen.");
                    return CreateValidationError("InternalError", new Dictionary<string, object>
                    {
                        ["Message"]="Lỗi nội bộ: Không thể xác định stage tiếp theo"
                    });
                }

                if (input.StageId.HasValue&&input.StageId.Value!=nextStageId)
                {
                    var requestedStage = stages.FirstOrDefault(s => s.StageId==input.StageId.Value);
                    var allowedStage = stages.FirstOrDefault(s => s.StageId==nextStageId);

                    return CreateValidationError("InvalidStageForNextStep", new Dictionary<string, object>
                    {
                        ["RequestedStageName"]=requestedStage?.StageName??"Unknown",
                        ["AllowedStageName"]=allowedStage?.StageName??"Unknown",
                        ["AllowedOrderIndex"]=allowedStage?.OrderIndex??0
                    });
                }

                var stageParameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                    p => p.Progress.StageId==nextStageId&&!p.IsDeleted,
                    include: q => q.Include(p => p.Progress)
                );

                var progress = new ProcessingBatchProgress
                {
                    ProgressId=Guid.NewGuid(),
                    BatchId=batchId,
                    StepIndex=nextStepIndex,
                    StageId=nextStageId,
                    StageDescription=isRetryScenario ? $"Retry lần {nextStepIndex-existingProgresses.Count(p => p.StageId==nextStageId)}" : "",
                    ProgressDate=input.ProgressDate,
                    OutputQuantity=input.OutputQuantity,
                    OutputUnit=string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                    PhotoUrl=input.PhotoUrl,
                    VideoUrl=input.VideoUrl,
                    CreatedAt=DateTime.UtcNow,
                    UpdatedAt=DateTime.UtcNow,
                    UpdatedBy=batch.FarmerId,
                    IsDeleted=false,
                    ProcessingParameters=new List<ProcessingParameter>()
                };

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);
                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine($"DEBUG: Input parameters count: {input.Parameters?.Count??0}");
                Console.WriteLine($"DEBUG: Stage parameters count: {stageParameters?.Count??0}");

                var parametersToCreate = new List<ProcessingParameter>();

                if (input.Parameters?.Any()==true)
                {
                    Console.WriteLine($"DEBUG: Creating {input.Parameters.Count} parameters from input for progress {progress.ProgressId}");

                    parametersToCreate=input.Parameters.Select(p => new ProcessingParameter
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=progress.ProgressId,
                        ParameterName=p.ParameterName,
                        ParameterValue=p.ParameterValue,
                        Unit=p.Unit,
                        RecordedAt=p.RecordedAt??DateTime.UtcNow,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    }).ToList();
                }
                else if (stageParameters?.Any()==true)
                {
                    Console.WriteLine($"DEBUG: Creating {stageParameters.Count} default parameters for progress {progress.ProgressId}");

                    parametersToCreate=stageParameters.Select(p => new ProcessingParameter
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=progress.ProgressId,
                        ParameterName=p.ParameterName,
                        Unit=p.Unit,
                        ParameterValue=null,
                        RecordedAt=null,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    }).ToList();
                }

                if (parametersToCreate.Any())
                {
                    Console.WriteLine($"DEBUG: Creating {parametersToCreate.Count} parameters total");

                    foreach (var param in parametersToCreate)
                    {
                        Console.WriteLine($"DEBUG: Creating parameter: {param.ParameterName} = {param.ParameterValue} {param.Unit}");
                        await _unitOfWork.ProcessingParameterRepository.CreateAsync(param);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG: Parameters saved successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG: No parameters to create (no input and no stage parameters)");
                }

                Console.WriteLine($"DEBUG CREATE: Processing workflow - isLastStep: {isLastStep}");

                if (!existingProgresses.Any())
                {
                    if (batch.Status==ProcessingStatus.NotStarted.ToString())
                    {
                        Console.WriteLine($"DEBUG CREATE: First step - changing status from NotStarted to InProgress");
                        batch.Status=ProcessingStatus.InProgress.ToString();
                        batch.UpdatedAt=DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }
                }
                else if (isRetryScenario)
                {
                    Console.WriteLine($"DEBUG CREATE: Processing retry scenario for stage {nextStageId}");

                    if (batch.Status=="AwaitingEvaluation")
                    {
                        Console.WriteLine($"DEBUG CREATE: Retry - changing status from AwaitingEvaluation to InProgress");
                        batch.Status="InProgress";
                        batch.UpdatedAt=DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }

                    if (isLastStep)
                    {
                        Console.WriteLine($"DEBUG CREATE: Retry last step - changing status to AwaitingEvaluation");
                        batch.Status="AwaitingEvaluation";
                        batch.UpdatedAt=DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                        var evaluation = new ProcessingBatchEvaluation
                        {
                            EvaluationId=Guid.NewGuid(),
                            BatchId=batchId,
                            EvaluatedBy=null,
                            EvaluatedAt=null,
                            EvaluationResult=null,
                            Comments=$"Retry evaluation sau khi sửa lỗi: {stages.First(s => s.StageId==nextStageId).StageName}",
                            CreatedAt=DateTime.UtcNow,
                            UpdatedAt=DateTime.UtcNow,
                            IsDeleted=false
                        };

                        await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                        Console.WriteLine($"DEBUG CREATE: Created new evaluation for retry scenario");
                    }
                }
                else if (isLastStep)
                {
                    Console.WriteLine($"DEBUG CREATE: Last step - changing status to AwaitingEvaluation");
                    batch.Status="AwaitingEvaluation";
                    batch.UpdatedAt=DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId=Guid.NewGuid(),
                        BatchId=batchId,
                        EvaluatedBy=null,
                        EvaluatedAt=null,
                        EvaluationResult=null,
                        Comments=$"Tự động tạo evaluation khi hoàn thành bước cuối cùng: {stages.Last().StageName}",
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                    Console.WriteLine($"DEBUG CREATE: Created evaluation for last step");
                }

                var result = await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"DEBUG CREATE: SaveChangesAsync result: {result}");

                var responseMessage = isRetryScenario
                    ? $"Đã tạo bước retry cho stage {stages.First(s => s.StageId==nextStageId).StageName} thành công."
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
            var entity = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(
                p => p.ProgressId==progressId&&!p.IsDeleted
            );

            if (entity==null)
                return CreateValidationError("ProgressNotFound", new Dictionary<string, object>
                {
                    ["ProgressId"]=progressId.ToString()
                });

            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(entity.BatchId);
            if (batch==null)
                return CreateValidationError("BatchNotFoundForProgress", new Dictionary<string, object>
                {
                    ["BatchId"]=entity.BatchId.ToString()
                });

            if (batch.Status!="InProgress")
                return CreateValidationError("CannotUpdateProgressBatchNotInProgress", new Dictionary<string, object>
                {
                    ["CurrentStatus"]=batch.Status
                });

            if (dto.StepIndex!=entity.StepIndex)
            {
                var isDuplicated = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    p => p.BatchId==entity.BatchId&&
                         p.StepIndex==dto.StepIndex&&
                         p.ProgressId!=progressId&&
                         !p.IsDeleted
                );

                if (isDuplicated)
                    return CreateValidationError("StepIndexAlreadyExists", new Dictionary<string, object>
                    {
                        ["StepIndex"]=dto.StepIndex,
                        ["BatchId"]=entity.BatchId.ToString()
                    });
            }

            bool isModified = false;

            if (entity.StepIndex!=dto.StepIndex)
            {
                entity.StepIndex=dto.StepIndex;
                isModified=true;
            }

            if (entity.OutputQuantity!=dto.OutputQuantity)
            {
                entity.OutputQuantity=dto.OutputQuantity;
                isModified=true;
            }

            if (!string.Equals(entity.OutputUnit, dto.OutputUnit, StringComparison.OrdinalIgnoreCase))
            {
                entity.OutputUnit=dto.OutputUnit;
                isModified=true;
            }

            var dtoDateOnly = DateOnly.FromDateTime(dto.ProgressDate);
            if (entity.ProgressDate!=dtoDateOnly)
            {
                entity.ProgressDate=dtoDateOnly;
                isModified=true;
            }

            if (!isModified)
            {
                return CreateValidationError("NoDataModified", new Dictionary<string, object>
                {
                    ["ProgressId"]=progressId.ToString()
                });
            }

            entity.UpdatedAt=DateTime.UtcNow;

            var updated = await _unitOfWork.ProcessingBatchProgressRepository.UpdateAsync(entity);
            if (!updated)
                return CreateValidationError("UpdateFailed", new Dictionary<string, object>
                {
                    ["ProgressId"]=progressId.ToString()
                });
            var result = await _unitOfWork.SaveChangesAsync();

            if (result>0)
            {
                var resultDto = entity.MapToProcessingBatchProgressDetailDto();

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, Const.SUCCESS_UPDATE_MSG, resultDto);
            }
            else
            {
                return CreateValidationError("NoChangesSaved", new Dictionary<string, object>
                {
                    ["ProgressId"]=progressId.ToString()
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
                        ["ProgressId"]=progressId.ToString()
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message??dbEx.Message;
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

                if (batchId==Guid.Empty)
                    return CreateValidationError("InvalidBatchId", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Looking for farmer with userId: {userId}");
                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId==userId&&!f.IsDeleted)).FirstOrDefault();
                if (farmer==null)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Farmer not found for userId: {userId}");
                    return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                    {
                        ["UserId"]=userId.ToString()
                    });
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found farmer: {farmer.FarmerId}");

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting batch: {batchId}");
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null||batch.IsDeleted)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Batch not found or deleted: {batchId}");
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found batch: {batch.BatchId}, status: {batch.Status}, farmerId: {batch.FarmerId}");

                if (batch.FarmerId!=farmer.FarmerId)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Permission denied - batch farmer: {batch.FarmerId}, current farmer: {farmer.FarmerId}");
                    return CreateValidationError("NoPermissionToUpdateBatch", new Dictionary<string, object>
                    {
                        ["UserId"]=userId.ToString(),
                        ["BatchId"]=batchId.ToString()
                    });
                }

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting stages for methodId: {batch.MethodId}");
                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count==0)
                {
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: No stages found for methodId: {batch.MethodId}");
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"]=batch.MethodId.ToString()
                    });
                }
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found {stages.Count} stages");

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Getting latest progress for batchId: {batchId}");
                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                ProcessingBatchProgress? latestProgress = progresses.FirstOrDefault();
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found {progresses.Count} progresses, latest: {(latestProgress!=null ? $"stepIndex: {latestProgress.StepIndex}, stageId: {latestProgress.StageId}" : "none")}");

                // 🔧 MỚI: Tìm stage cuối cùng được Retry thành công để tiếp tục Advance từ đó.
                var lastRetryProgress = progresses
                    .Where(p => p.StageDescription!=null&&
                               (p.StageDescription.Contains("Làm lại (Retry)")||
                                p.StageDescription.Contains("Retry")))
                    .OrderByDescending(p => p.StepIndex)
                    .FirstOrDefault();

                if (lastRetryProgress!=null)
                {
                    // Lấy Stage Order của Retry cuối cùng
                    var retryStageOrder = stages.FirstOrDefault(s => s.StageId==lastRetryProgress.StageId)?.OrderIndex??0;

                    // Lấy StepIndex cuối cùng của Stage đó
                    var lastProgressOfRetryStage = progresses.Where(p => p.StageId==lastRetryProgress.StageId).OrderByDescending(p => p.StepIndex).FirstOrDefault();

                    if (lastProgressOfRetryStage!=null&&lastProgressOfRetryStage.StageId==latestProgress?.StageId)
                    {
                        // Nếu Stage cuối cùng là Stage vừa được Retry, thì tiếp tục Advance sang Stage kế tiếp
                        int currentStageIdx = stages.FindIndex(s => s.StageId==lastRetryProgress.StageId);

                        if (currentStageIdx<stages.Count-1)
                        {
                            var nextStageAfterRetry = stages[currentStageIdx+1];
                            latestProgress=lastRetryProgress;

                            // Ghi đè logic để tìm bước tiếp theo là Stage Order Index + 1
                            latestProgress.StageId=nextStageAfterRetry.StageId;
                            // Reset StepIndex để tránh lỗi Duplicate Key, Advance luôn tính StepIndex mới nhất + 1.
                            // Logic này sẽ được tính toán lại dưới đây:
                        }
                    }
                }


                int nextStepIndex = 0;
                ProcessingStage? nextStage = null;
                bool isLastStep = false;

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Using stageId from input: {input.StageId}");

                // 🔧 MỚI: Kiểm tra xem có retry progress không để cho phép advance tiếp
                var hasRetryProgress = progresses.Any(p => 
                    p.StageDescription != null && p.StageDescription.Contains("Làm lại (Retry)"));
                
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Auto-calculating next stage... hasRetryProgress: {hasRetryProgress}");

                if (latestProgress==null)
                {
                    nextStepIndex=1;
                    nextStage=stages.FirstOrDefault();
                    isLastStep=(stages.Count==1);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: No previous progress, starting with stepIndex: {nextStepIndex}, stageId: {nextStage?.StageId}, isLastStep: {isLastStep}");
                }
                else
                {
                    int currentStageIdx = stages.FindIndex(s => s.StageId==latestProgress.StageId);
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Current stage index: {currentStageIdx}, total stages: {stages.Count}");
                    Console.WriteLine($"DEBUG SERVICE ADVANCE: Latest progress stage: {latestProgress.StageId}, stage description: {latestProgress.StageDescription}");

                    if (currentStageIdx==-1)
                    {
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Current stage not found in stages list");
                        return CreateValidationError("CurrentStageNotFound", new Dictionary<string, object>
                        {
                            ["StageId"]=latestProgress.StageId.ToString()
                        });
                    }

                    if (currentStageIdx>=stages.Count-1 && !hasRetryProgress)
                    {
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Already at last stage and no retry, cannot advance further");
                        return CreateValidationError("AllStepsCompletedCannotAdvance", new Dictionary<string, object>
                        {
                            ["CurrentStageName"]=stages[currentStageIdx].StageName
                        });
                    }
                    
                    // Xử lý trường hợp bình thường (không phải stage cuối hoặc có retry)
                    if (currentStageIdx < stages.Count - 1)
                    {
                        nextStepIndex=latestProgress.StepIndex+1;
                        nextStage=stages[currentStageIdx+1];

                        var maxOrderIndex = stages.Max(s => s.OrderIndex);
                        isLastStep=(nextStage.OrderIndex>=maxOrderIndex);

                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Next stepIndex: {nextStepIndex}, nextStageId: {nextStage?.StageId}, isLastStep: {isLastStep}");
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: Next stage OrderIndex: {nextStage.OrderIndex}, Max OrderIndex: {maxOrderIndex}");
                    }
                    // Xử lý trường hợp có retry progress và cần advance tiếp
                    else if (hasRetryProgress)
                    {
                        Console.WriteLine($"DEBUG SERVICE ADVANCE: At last stage but has retry progress, finding next stage after retry");
                        
                        // Tìm stage cuối cùng được retry
                        var lastRetryStage = progresses
                            .Where(p => p.StageDescription != null && p.StageDescription.Contains("Làm lại (Retry)"))
                            .OrderByDescending(p => p.StepIndex)
                            .FirstOrDefault();
                        
                        if (lastRetryStage != null)
                        {
                            var retryStageOrder = stages.FirstOrDefault(s => s.StageId == lastRetryStage.StageId)?.OrderIndex ?? 0;
                            Console.WriteLine($"DEBUG SERVICE ADVANCE: Last retry stage order: {retryStageOrder}");
                            
                            // Tìm stage tiếp theo sau stage retry
                            var nextStageAfterRetry = stages.FirstOrDefault(s => s.OrderIndex == retryStageOrder + 1);
                            
                            if (nextStageAfterRetry != null)
                            {
                                nextStepIndex = latestProgress.StepIndex + 1;
                                nextStage = nextStageAfterRetry;
                                
                                var maxOrderIndex = stages.Max(s => s.OrderIndex);
                                isLastStep = (nextStage.OrderIndex >= maxOrderIndex);
                                
                                Console.WriteLine($"DEBUG SERVICE ADVANCE: Found next stage after retry - stepIndex: {nextStepIndex}, stageId: {nextStage.StageId}, isLastStep: {isLastStep}");
                            }
                        }
                    }
                }

                if (nextStage==null)
                    return CreateValidationError("NextStageNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });

                if (input.OutputQuantity.HasValue)
                {
                    if (input.OutputQuantity.Value<=0)
                    {
                        return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                        {
                            ["OutputQuantity"]=input.OutputQuantity.Value,
                            ["MinValue"]=0
                        });
                    }

                    var batchInputUnit = string.IsNullOrWhiteSpace(batch.InputUnit) ? "kg" : batch.InputUnit.Trim().ToLower();
                    var advanceOutputUnit = string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit.Trim().ToLower();

                    if (batchInputUnit==advanceOutputUnit&&input.OutputQuantity.Value>=batch.InputQuantity)
                    {
                        return CreateValidationError("OutputQuantityExceedsInputQuantity", new Dictionary<string, object>
                        {
                            ["OutputQuantity"]=input.OutputQuantity.Value,
                            ["OutputUnit"]=input.OutputUnit??"kg",
                            ["InputQuantity"]=batch.InputQuantity,
                            ["InputUnit"]=batch.InputUnit
                        });
                    }

                    if (latestProgress!=null&&latestProgress.OutputQuantity.HasValue)
                    {
                        var previousQuantity = latestProgress.OutputQuantity.Value;
                        var currentQuantity = input.OutputQuantity.Value;
                        var changePercentage = ((currentQuantity-previousQuantity)/previousQuantity)*100;

                        Console.WriteLine($"DEBUG ADVANCE: Quantity comparison:");
                        Console.WriteLine($"  - Previous quantity: {previousQuantity} {latestProgress.OutputUnit}");
                        Console.WriteLine($"  - Current quantity: {currentQuantity} {input.OutputUnit}");
                        Console.WriteLine($"  - Change: {changePercentage:F2}%");

                        const double tolerance = 0.15;

                        if (currentQuantity>previousQuantity*(1+tolerance))
                        {
                            return CreateValidationError("OutputQuantityIncreaseTooHigh", new Dictionary<string, object>
                            {
                                ["CurrentQuantity"]=currentQuantity,
                                ["CurrentUnit"]=input.OutputUnit??"kg",
                                ["PreviousQuantity"]=previousQuantity,
                                ["PreviousUnit"]=latestProgress.OutputUnit,
                                ["Tolerance"]=tolerance*100,
                                ["IncreasePercentage"]=changePercentage
                            });
                        }

                        if (changePercentage<-70)
                        {
                            return CreateValidationError("OutputQuantityDecreaseTooHigh", new Dictionary<string, object>
                            {
                                ["CurrentQuantity"]=currentQuantity,
                                ["CurrentUnit"]=input.OutputUnit??"kg",
                                ["PreviousQuantity"]=previousQuantity,
                                ["PreviousUnit"]=latestProgress.OutputUnit,
                                ["DecreasePercentage"]=Math.Abs(changePercentage)
                            });
                        }
                    }
                }

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Creating new progress - stepIndex: {nextStepIndex}, stageId: {nextStage.StageId}, hasRetryProgress: {hasRetryProgress}");
                var newProgress = new ProcessingBatchProgress
                {
                    ProgressId=Guid.NewGuid(),
                    BatchId=batch.BatchId,
                    StepIndex=nextStepIndex,
                    StageId=nextStage.StageId,
                    StageDescription=!string.IsNullOrWhiteSpace(input.StageDescription) ? input.StageDescription : 
                                   (hasRetryProgress ? $"Làm lại (Retry) - {nextStage.StageName}" : (nextStage.Description??"")),
                    ProgressDate=input.ProgressDate??DateOnly.FromDateTime(DateTime.UtcNow),
                    OutputQuantity=input.OutputQuantity,
                    OutputUnit=string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                    PhotoUrl=input.PhotoUrl,
                    VideoUrl=input.VideoUrl,
                    CreatedAt=DateTime.UtcNow,
                    UpdatedAt=DateTime.UtcNow,
                    UpdatedBy=farmer.FarmerId,
                    IsDeleted=false,
                    ProcessingParameters=new List<ProcessingParameter>()
                };

                Console.WriteLine($"DEBUG SERVICE ADVANCE: Saving progress to database...");
                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(newProgress);
                await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"DEBUG SERVICE ADVANCE: Progress saved successfully with ID: {newProgress.ProgressId}");

                var stageParameters = await _unitOfWork.ProcessingParameterRepository.GetAllAsync(
                    p => p.Progress.StageId==nextStage.StageId&&!p.IsDeleted,
                    include: q => q.Include(p => p.Progress)
                );

                Console.WriteLine($"DEBUG ADVANCE: Input parameters count: {input.Parameters?.Count??0}");
                Console.WriteLine($"DEBUG ADVANCE: Stage parameters count: {stageParameters?.Count??0}");

                var parametersToCreate = new List<ProcessingParameter>();

                if (input.Parameters?.Any()==true)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Creating {input.Parameters.Count} parameters from input for progress {newProgress.ProgressId}");

                    parametersToCreate=input.Parameters.Select(p => new ProcessingParameter
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=newProgress.ProgressId,
                        ParameterName=p.ParameterName,
                        ParameterValue=p.ParameterValue,
                        Unit=p.Unit,
                        RecordedAt=p.RecordedAt??DateTime.UtcNow,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    }).ToList();
                }
                else if (stageParameters?.Any()==true)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Creating {stageParameters.Count} default parameters for progress {newProgress.ProgressId}");

                    parametersToCreate=stageParameters.Select(p => new ProcessingParameter
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=newProgress.ProgressId,
                        ParameterName=p.ParameterName,
                        Unit=p.Unit,
                        ParameterValue=null,
                        RecordedAt=null,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    }).ToList();
                }

                if (parametersToCreate.Any())
                {
                    Console.WriteLine($"DEBUG ADVANCE: Creating {parametersToCreate.Count} parameters total");

                    foreach (var param in parametersToCreate)
                    {
                        Console.WriteLine($"DEBUG ADVANCE: Creating parameter: {param.ParameterName} = {param.ParameterValue} {param.Unit}");
                        await _unitOfWork.ProcessingParameterRepository.CreateAsync(param);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG ADVANCE: Parameters saved successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG ADVANCE: No parameters to create (no input and no stage parameters)");
                }

                bool hasChanges = false;

                if (latestProgress==null)
                {
                    if (batch.Status==ProcessingStatus.NotStarted.ToString())
                    {
                        batch.Status=ProcessingStatus.InProgress.ToString();
                        batch.UpdatedAt=DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                        hasChanges=true;
                    }
                }
                else if (isLastStep)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Updating batch status from '{batch.Status}' to 'AwaitingEvaluation'");
                    batch.Status="AwaitingEvaluation";
                    batch.UpdatedAt=DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    Console.WriteLine($"DEBUG ADVANCE: Batch status updated successfully");

                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId=Guid.NewGuid(),
                        EvaluationCode=await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year),
                        BatchId=batchId,
                        EvaluatedBy=null,
                        EvaluatedAt=null,
                        EvaluationResult=null,
                        Comments=$"Tự động tạo evaluation khi hoàn thành bước cuối cùng: {stages.Last().StageName}",
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);
                    hasChanges=true;
                    Console.WriteLine($"DEBUG ADVANCE: Created new evaluation for batch with code: {evaluation.EvaluationCode}");
                }

                var responseMessage = isLastStep
                    ? "Đã tạo bước cuối cùng và chuyển sang chờ đánh giá từ chuyên gia."
                    : "Đã tạo bước tiến trình kế tiếp.";

                Console.WriteLine($"DEBUG ADVANCE: Response message: {responseMessage}");
                Console.WriteLine($"DEBUG ADVANCE: Is last step: {isLastStep}");
                Console.WriteLine($"DEBUG ADVANCE: Has changes: {hasChanges}");

                if (hasChanges)
                {
                    Console.WriteLine($"DEBUG ADVANCE: Final save changes...");
                    try
                    {
                        var saveResult = await _unitOfWork.SaveChangesAsync();
                        Console.WriteLine($"DEBUG ADVANCE: Save result: {saveResult}");
                        Console.WriteLine($"DEBUG ADVANCE: Save result > 0: {saveResult>0}");

                        if (saveResult>0)
                        {
                            Console.WriteLine($"DEBUG ADVANCE: Returning success response");
                            return new ServiceResult(Const.SUCCESS_CREATE_CODE, responseMessage);
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG ADVANCE: Save failed - no changes saved");
                            return CreateValidationError("CannotCreateNextStep", new Dictionary<string, object>
                            {
                                ["BatchId"]=batchId.ToString()
                            });
                        }
                    }
                    catch (Exception saveEx)
                    {
                        Console.WriteLine($"DEBUG ADVANCE: Exception during SaveChangesAsync: {saveEx.Message}");
                        Console.WriteLine($"DEBUG ADVANCE: Exception stack trace: {saveEx.StackTrace}");

                        if (saveEx.InnerException!=null)
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

                if (batchId==Guid.Empty)
                    return CreateValidationError("InvalidBatchId", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });

                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId==userId&&!f.IsDeleted)).FirstOrDefault();
                if (farmer==null)
                {
                    return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                    {
                        ["UserId"]=userId.ToString()
                    });
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null||batch.IsDeleted)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (batch.FarmerId!=farmer.FarmerId)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Permission denied - batch farmer: {batch.FarmerId}, current farmer: {farmer.FarmerId}");
                    return CreateValidationError("NoPermissionToUpdateBatch", new Dictionary<string, object>
                    {
                        ["UserId"]=userId.ToString(),
                        ["BatchId"]=batchId.ToString()
                    });
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Permission granted - using farmer: {farmer.FarmerId}");

                var farmerExists = await _unitOfWork.FarmerRepository.AnyAsync(f => f.FarmerId==farmer.FarmerId&&!f.IsDeleted);
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Farmer exists in database: {farmerExists}");

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Batch status: '{batch.Status}'");

                if (batch.Status!="InProgress"&&batch.Status!="AwaitingEvaluation")
                {
                    return CreateValidationError("CannotUpdateProgressBatchNotInProgress", new Dictionary<string, object>
                    {
                        ["CurrentStatus"]=batch.Status
                    });
                }

                var allEvaluations = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId==batchId&&!e.IsDeleted,
                    q => q.OrderByDescending(e => e.CreatedAt)
                );

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {allEvaluations.Count} evaluations for batch {batchId}");

                foreach (var eval in allEvaluations)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation - ID: {eval.EvaluationId}, Result: '{eval.EvaluationResult}', CreatedAt: {eval.CreatedAt}, UpdatedAt: {eval.UpdatedAt}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Comments: {eval.Comments}");
                }

                var evaluation = allEvaluations.FirstOrDefault(e => e.EvaluationResult=="Fail");

                if (evaluation!=null)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found FAIL evaluation - ID: {evaluation.EvaluationId}, Result: '{evaluation.EvaluationResult}', CreatedAt: {evaluation.CreatedAt}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Evaluation comments: {evaluation.Comments}");
                }

                if (evaluation==null)
                {
                    return CreateValidationError("NoFailedEvaluationFoundForBatch", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                var failedStages = await _evaluationService.GetFailedStagesForBatchAsync(batchId);
                if (failedStages==null||failedStages.Count==0)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: No failed stages found for batch {batchId}");
                    return CreateValidationError("NoFailedStagesToUpdate", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {failedStages.Count} failed stages: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");

                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count==0)
                {
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"]=batch.MethodId.ToString()
                    });
                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Batch MethodId: {batch.MethodId}");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {stages.Count} stages for MethodId {batch.MethodId}:");
                foreach (var stage in stages)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Stage - ID: {stage.StageId}, Name: {stage.StageName}, OrderIndex: {stage.OrderIndex}");
                }

                ProcessingStage currentStage = null;

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Input.StageId from frontend: {input.StageId}");
                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Failed stages available: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");

                // LẤY MIN FAILED ORDER TRƯỚC VÀ KIỂM TRA ĐIỀU KIỆN 
                var failedStageOrders = failedStages.Select(s => s.OrderIndex).ToList();
                var minFailedOrder = failedStageOrders.Any() ? failedStageOrders.Min() : 0;

                // FIX: Sử dụng OrderIndex từ frontend để tìm stage thực sự bị fail
                // Frontend gửi OrderIndex (5) thay vì StageId
                if (input.StageId.HasValue)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Frontend sent OrderIndex: {input.StageId.Value}");

                    // Tìm stage trong failedStages có OrderIndex trùng với input.StageId
                    currentStage=failedStages.FirstOrDefault(s => s.OrderIndex==input.StageId.Value);

                    if (currentStage!=null)
                    {
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found failed stage by OrderIndex: {currentStage.StageName} (ID: {currentStage.StageId}, OrderIndex: {currentStage.OrderIndex})");
                    }
                    else
                    {
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: No failed stage found with OrderIndex {input.StageId.Value}");
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Available failed stages: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (OrderIndex: {s.OrderIndex})"))}");

                        // Fallback: Sử dụng stage đầu tiên từ failedStages
                        currentStage=failedStages.OrderBy(s => s.OrderIndex).FirstOrDefault();
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Fallback to first failed stage: {currentStage?.StageName} (ID: {currentStage?.StageId})");
                    }
                }
                else
                {
                    // Fallback: Sử dụng stage đầu tiên từ failedStages nếu không có input.StageId
                    currentStage=failedStages.OrderBy(s => s.OrderIndex).FirstOrDefault();
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: No OrderIndex provided, using first failed stage: {currentStage?.StageName} (ID: {currentStage?.StageId})");
                }

                if (currentStage==null)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: ERROR - No valid stage found for update!");
                    return CreateValidationError("NoValidStageForUpdate", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString(),
                        ["MethodId"]=batch.MethodId.ToString(),
                        ["FailedStagesCount"]=failedStages.Count
                    });
                }

                var stageExists = stages.Any(s => s.StageId==currentStage.StageId);
                if (!stageExists)
                {
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: ERROR - StageId {currentStage.StageId} not found in stages list!");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Available StageIds: {string.Join(", ", stages.Select(s => s.StageId))}");
                    return CreateValidationError("CurrentStageNotFound", new Dictionary<string, object>
                    {
                        ["StageId"]=currentStage.StageId.ToString(),
                        ["MethodId"]=batch.MethodId.ToString(),
                        ["AvailableStageIds"]=string.Join(", ", stages.Select(s => s.StageId))
                    });

                }

                Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Using stage: {currentStage.StageName} (ID: {currentStage.StageId}) - {(failedStages.Any(s => s.StageId==currentStage.StageId) ? "from failed stages" : "from all stages")}");

                if (!input.OutputQuantity.HasValue||input.OutputQuantity.Value<=0)
                {
                    return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                    {
                        ["OutputQuantity"]=input.OutputQuantity?.ToString()??"null",
                        ["MinValue"]=0
                    });
                }

                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                var latestProgress = progresses.FirstOrDefault();
                int nextStepIndex;

                var currentStageProgress = progresses.FirstOrDefault(p => p.StageId==currentStage.StageId);

                if (currentStageProgress!=null)
                {
                    nextStepIndex=currentStageProgress.StepIndex+1;
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Retry stage {currentStage.StageName} - Current StepIndex: {currentStageProgress.StepIndex}, Next StepIndex: {nextStepIndex}");
                }
                else
                {
                    nextStepIndex=latestProgress!=null ? latestProgress.StepIndex+1 : 1;
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: New stage {currentStage.StageName} - Next StepIndex: {nextStepIndex}");
                }

                var stagesOrderedForValidation = stages.OrderBy(s => s.OrderIndex).ToList();
                var currentStageIndexForValidation = stagesOrderedForValidation.FindIndex(s => s.StageId==currentStage.StageId);

                if (currentStageIndexForValidation>0)
                {
                    var previousStageForValidation = stagesOrderedForValidation[currentStageIndexForValidation-1];
                    var previousStageProgressForValidation = progresses.FirstOrDefault(p => p.StageId==previousStageForValidation.StageId);

                    if (previousStageProgressForValidation!=null&&previousStageProgressForValidation.OutputQuantity.HasValue)
                    {
                        var isCurrentStageFailed = failedStages.Any(failedStage =>
                            failedStage.StageId==currentStage.StageId
                        );

                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Stage {currentStage.StageName} is failed: {isCurrentStageFailed}");
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Previous stage: {previousStageForValidation.StageName}");

                        if (isCurrentStageFailed)
                        {
                            // 🔧 TEMPORARY: Comment out waste validation để test logic mới
                            /*
                            var validationResult = await ValidateRetryQuantityWithWasteAsync(
                                currentStage,
                                input.OutputQuantity.Value,
                                input.OutputUnit??"kg",
                                previousStageProgressForValidation.OutputQuantity.Value,
                                previousStageProgressForValidation.OutputUnit??"kg",
                                batchId
                            );

                            if (!validationResult.IsValid)
                            {
                                return CreateValidationError(validationResult.ErrorCode, validationResult.ErrorParameters);
                            }
                            */
                            Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Skipping waste validation for failed stage: {currentStage.StageName}");
                        }
                        else
                        {
                            var basicValidationResult = await ValidateNonFailedStageQuantityAsync(
                                currentStage,
                                input.OutputQuantity.Value,
                                input.OutputUnit??"kg",
                                previousStageProgressForValidation.OutputQuantity.Value,
                                previousStageProgressForValidation.OutputUnit??"kg"
                            );

                            if (!basicValidationResult.IsValid)
                            {
                                return CreateValidationError(basicValidationResult.ErrorCode, basicValidationResult.ErrorParameters);
                            }
                        }
                    }

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Latest progress: {(latestProgress!=null ? $"StepIndex: {latestProgress.StepIndex}, StageId: {latestProgress.StageId}" : "none")}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Next StepIndex: {nextStepIndex}");

                    var existingStepIndex = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                        p => p.BatchId==batchId&&p.StepIndex==nextStepIndex&&!p.IsDeleted
                    );
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: StepIndex {nextStepIndex} already exists: {existingStepIndex}");

                    double wasteQuantity = 0;
                    string wasteUnit = "kg";

                    // 🔧 FIX: Sử dụng logic đúng để lấy output cuối cùng trước retry
                    var isCurrentStageFailedForWaste = failedStages.Any(failedStage => failedStage.StageId==currentStage.StageId);

                    if (isCurrentStageFailedForWaste)
                    {
                        // Đối với stage bị fail, lấy output cuối cùng của chính stage đó trước khi fail
                        var lastProgressOfFailedStage = progresses
                            .Where(p => p.StageId==currentStage.StageId)
                            .OrderByDescending(p => p.StepIndex)
                            .FirstOrDefault();

                        if (lastProgressOfFailedStage!=null&&lastProgressOfFailedStage.OutputQuantity.HasValue)
                        {
                            var finalOutputBeforeRetry = await NormalizeQuantityAsync(lastProgressOfFailedStage.OutputQuantity.Value, lastProgressOfFailedStage.OutputUnit??"kg");
                            var retryQuantityInKgForWaste = await NormalizeQuantityAsync(input.OutputQuantity.Value, input.OutputUnit??"kg");

                            wasteQuantity=Math.Max(0, finalOutputBeforeRetry-retryQuantityInKgForWaste);

                            Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Stage: {currentStage.StageName} (FAILED STAGE)");
                            Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Final output before retry: {finalOutputBeforeRetry} kg");
                            Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Retry quantity: {retryQuantityInKgForWaste} kg");
                            Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Auto-calculated waste: {wasteQuantity} kg");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: No previous progress found for failed stage, waste = 0");
                        }
                    }
                    else
                    {
                        // Đối với stage bình thường, lấy output của stage trước đó
                        var stagesOrderedForWaste = stages.OrderBy(s => s.OrderIndex).ToList();
                        var currentStageIndexForWaste = stagesOrderedForWaste.FindIndex(s => s.StageId==currentStage.StageId);

                        if (currentStageIndexForWaste>0)
                        {
                            var previousStageForWaste = stagesOrderedForWaste[currentStageIndexForWaste-1];
                            var previousStageProgressForWaste = progresses.FirstOrDefault(p => p.StageId==previousStageForWaste.StageId);

                            if (previousStageProgressForWaste!=null&&previousStageProgressForWaste.OutputQuantity.HasValue)
                            {
                                var previousQuantityInKgForWaste = await NormalizeQuantityAsync(previousStageProgressForWaste.OutputQuantity.Value, previousStageProgressForWaste.OutputUnit??"kg");
                                var retryQuantityInKgForWaste = await NormalizeQuantityAsync(input.OutputQuantity.Value, input.OutputUnit??"kg");

                                wasteQuantity=Math.Max(0, previousQuantityInKgForWaste-retryQuantityInKgForWaste);

                                Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Stage: {currentStage.StageName} (NORMAL STAGE)");
                                Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Previous stage: {previousStageForWaste.StageName}");
                                Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Previous quantity: {previousQuantityInKgForWaste} kg");
                                Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Current quantity: {retryQuantityInKgForWaste} kg");
                                Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: Auto-calculated waste: {wasteQuantity} kg");
                            }
                            else
                            {
                                Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: No previous stage progress found, waste = 0");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG AUTO WASTE CALCULATION: This is the first stage, waste = 0");
                        }
                    }

                    var progress = new ProcessingBatchProgress
                    {
                        ProgressId=Guid.NewGuid(),
                        BatchId=batchId,
                        StepIndex=nextStepIndex,
                        StageId=currentStage.StageId,
                        StageDescription=$"Làm lại (Retry) - {currentStage.StageName}",
                        ProgressDate=input.ProgressDate,
                        OutputQuantity=input.OutputQuantity,
                        OutputUnit=string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                        PhotoUrl=input.PhotoUrl,
                        VideoUrl=input.VideoUrl,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        UpdatedBy=farmer.FarmerId,
                        IsDeleted=false,
                        ProcessingParameters=new List<ProcessingParameter>()
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

                    await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);

                    // 🔧 MỚI: Log thông tin về các stage cần làm tiếp
                    var currentStageOrderIndex = currentStage.OrderIndex;
                    var subsequentStages = stages.Where(s => s.OrderIndex>currentStageOrderIndex).ToList();

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {subsequentStages.Count} subsequent stages after retry of {currentStage.StageName}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Subsequent stages: {string.Join(", ", subsequentStages.Select(s => s.StageName))}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Farmer will manually update next stages using the form");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Batch status will be updated after completing all subsequent stages");

                    if (wasteQuantity>0)
                    {
                        Console.WriteLine($"DEBUG WASTE CREATION: Creating waste record for progress {progress.ProgressId}");

                        var wasteCode = $"WASTE-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                        var wasteRecord = new ProcessingBatchWaste
                        {
                            WasteId=Guid.NewGuid(),
                            WasteCode=wasteCode,
                            ProgressId=progress.ProgressId,
                            WasteType="Phế phẩm chế biến",
                            Quantity=wasteQuantity,
                            Unit=wasteUnit,
                            Note=$"Waste tự động tính từ retry stage {currentStage.StageName} - Waste: {wasteQuantity} kg",
                            RecordedAt=DateTime.UtcNow,
                            RecordedBy=farmer.FarmerId,
                            CreatedAt=DateTime.UtcNow,
                            UpdatedAt=DateTime.UtcNow,
                            IsDeleted=false
                        };

                        await _unitOfWork.ProcessingWasteRepository.CreateAsync(wasteRecord);
                        Console.WriteLine($"DEBUG WASTE CREATION: Created waste record: {wasteRecord.WasteId} - {wasteQuantity} kg");
                    }

                    if (input.Parameters?.Any()==true)
                    {
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Creating {input.Parameters.Count} parameters");
                        var parametersToCreate = input.Parameters.Select(p => new ProcessingParameter
                        {
                            ParameterId=Guid.NewGuid(),
                            ProgressId=progress.ProgressId,
                            ParameterName=p.ParameterName,
                            ParameterValue=p.ParameterValue,
                            Unit=p.Unit,
                            RecordedAt=p.RecordedAt??DateTime.UtcNow,
                            CreatedAt=DateTime.UtcNow,
                            UpdatedAt=DateTime.UtcNow,
                            IsDeleted=false
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

                    var allRetryProgresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                        p => p.BatchId==batchId&&
                             p.StageDescription!=null&&
                             p.StageDescription.Contains("Làm lại (Retry)")&&
                             !p.IsDeleted
                    );

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Found {allRetryProgresses.Count()} retry progresses");
                    foreach (var retryProgress in allRetryProgresses)
                    {
                        Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Retry progress - StageId: {retryProgress.StageId}, Description: {retryProgress.StageDescription}");
                    }

                    var retryStageIds = allRetryProgresses.Select(p => p.StageId).Distinct().ToList();
                    var failedStageIds = failedStages.Select(s => s.StageId).ToList();

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Retry stages completed: {string.Join(", ", retryStageIds)}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Failed stages total: {string.Join(", ", failedStageIds)}");

                    var remainingFailedStageIds = failedStageIds.Except(retryStageIds).ToList();
                    var allFailedStagesRetried = !remainingFailedStageIds.Any();

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Current retry for StageId: {currentStage.StageId}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Retry stages completed: {string.Join(", ", retryStageIds)}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Remaining stages to retry: {string.Join(", ", remainingFailedStageIds)}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: All failed stages retried: {allFailedStagesRetried}");

                    var currentStageOrderIndexForStatus = currentStage.OrderIndex;
                    var maxOrderIndex = stages.Max(s => s.OrderIndex);
                    var isCurrentStageLast = currentStageOrderIndexForStatus>=maxOrderIndex;

                    // 🔧 MỚI: Sử dụng validProgressesList đã được cập nhật sau invalidation
                    var allBatchEvaluations = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync();
                    var batchEvaluations = allBatchEvaluations.Where(e => e.BatchId==batchId&&!e.IsDeleted).ToList();

                    // 🔧 MỚI: Không xóa evaluation fail cũ để Frontend vẫn hiển thị
                    // if (evaluation!=null)
                    // {
                    //     evaluation.IsDeleted = true;
                    //     evaluation.UpdatedAt = DateTime.UtcNow;
                    //     await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(evaluation);
                    //     Console.WriteLine($"DEBUG FINAL STEP: Soft-deleted previous FAIL evaluation: {evaluation.EvaluationId}");
                    // }

                    // 2. ĐẶT TRẠNG THÁI CHẮC CHẮN VỀ INPROGRESS
                    batch.Status="InProgress";
                    batch.UpdatedAt=DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);

                    // 3. Nếu là Stage cuối cùng, tạo Evaluation mới để hoàn tất
                    if (isCurrentStageLast)
                    {
                        // Logic này sẽ được xử lý trong AdvanceProgressByBatchIdAsync khi hoàn thành Stage cuối cùng
                        // Tuy nhiên, nếu Stage vừa Retry là Stage cuối cùng, ta cần tạo Evaluation ngay
                        var newEvaluation = new ProcessingBatchEvaluation
                        {
                            EvaluationId=Guid.NewGuid(),
                            EvaluationCode=await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year),
                            BatchId=batchId,
                            EvaluationResult=null,
                            Comments=$"Tự động tạo evaluation sau khi hoàn thành Stage cuối cùng ({currentStage.StageName}) sau khi Retry.",
                            CreatedAt=DateTime.UtcNow,
                            UpdatedAt=DateTime.UtcNow,
                            IsDeleted=false
                        };
                        await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(newEvaluation);
                        batch.Status="AwaitingEvaluation";
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }


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
                        throw;
                    }

                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Successfully updated progress and created new evaluation");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Final batch status: {batch.Status}");
                    Console.WriteLine($"DEBUG UPDATE AFTER EVALUATION: Progress created with ID: {progress.ProgressId}");

                    var successMessage = isCurrentStageLast
                        ? $"Đã cập nhật progress cho stage {currentStage.StageName} (StageId: {currentStage.StageId}) và tạo đánh giá mới. Batch đã chuyển sang chờ đánh giá."
                        : $"Đã cập nhật progress cho stage {currentStage.StageName} (StageId: {currentStage.StageId}). Vui lòng tiếp tục các giai đoạn tiếp theo.";

                    return new ServiceResult(Const.SUCCESS_CREATE_CODE, successMessage, progress.ProgressId);
                }

                // FIX: Thêm return để đảm bảo tất cả code paths trả về giá trị (nếu logic bên trong bị lỗi)
                return new ServiceResult(Const.ERROR_EXCEPTION, "Lỗi logic nội bộ: Phương thức UpdateProgressAfterEvaluationAsync không trả về giá trị hợp lệ.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        private class RetryQuantityValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorCode { get; set; }
            public Dictionary<string, object> ErrorParameters { get; set; }
            public string Suggestion { get; set; }
        }

        private async Task<RetryQuantityValidationResult> ValidateRetryQuantityAsync(
            ProcessingStage currentStage,
            double currentQuantity,
            string currentUnit,
            double previousQuantity,
            string previousUnit)
        {
            var result = new RetryQuantityValidationResult { IsValid=true };

            var tolerance = GetToleranceForStage(currentStage.StageName);
            var warningThreshold = GetWarningThresholdForStage(currentStage.StageName);

            Console.WriteLine($"DEBUG RETRY VALIDATION: Stage: {currentStage.StageName}, Tolerance: {tolerance*100}%, Warning: {warningThreshold*100}%");

            var normalizedCurrentQuantity = await NormalizeQuantityAsync(currentQuantity, currentUnit);
            var normalizedPreviousQuantity = await NormalizeQuantityAsync(previousQuantity, previousUnit);

            var improvementPercentage = ((normalizedCurrentQuantity-normalizedPreviousQuantity)/normalizedPreviousQuantity)*100;

            Console.WriteLine($"DEBUG RETRY VALIDATION: Quantity comparison:");
            Console.WriteLine($"  - Previous: {previousQuantity} {previousUnit} (normalized: {normalizedPreviousQuantity} kg)");
            Console.WriteLine($"  - Current: {currentQuantity} {currentUnit} (normalized: {normalizedCurrentQuantity} kg)");
            Console.WriteLine($"  - Change: {improvementPercentage:F2}%");

            var validationRules = GetValidationRulesForStage(currentStage.StageName);

            foreach (var rule in validationRules)
            {
                var ruleResult = ValidateQuantityRule(rule, normalizedCurrentQuantity, normalizedPreviousQuantity, improvementPercentage);
                if (!ruleResult.IsValid)
                {
                    result.IsValid=false;
                    result.ErrorCode=ruleResult.ErrorCode;
                    result.ErrorParameters=ruleResult.ErrorParameters;
                    result.Suggestion=GetSuggestionForStage(currentStage.StageName, ruleResult.ErrorCode);
                    break;
                }
            }

            return result;
        }

        private double GetToleranceForStage(string stageName)
        {
            return stageName.ToLower() switch
            {
                "thu hoạch" => 0.15,
                "phơi" => 0.30,
                "xay vỏ" => 0.20,
                "rang" => 0.25,
                "đóng gói" => 0.10,
                _ => 0.25
            };
        }

        private double GetWarningThresholdForStage(string stageName)
        {
            return stageName.ToLower() switch
            {
                "thu hoạch" => 0.10,
                "phơi" => 0.20,
                "xay vỏ" => 0.15,
                "rang" => 0.20,
                "đóng gói" => 0.05,
                _ => 0.15
            };
        }

        private List<QuantityValidationRule> GetValidationRulesForStage(string stageName)
        {
            var rules = new List<QuantityValidationRule>();

            rules.Add(new QuantityValidationRule
            {
                Type=ValidationRuleType.MaxIncrease,
                Threshold=GetToleranceForStage(stageName),
                ErrorCode="OutputQuantityIncreaseTooHigh",
                ErrorMessage=$"Khối lượng tăng quá cao cho giai đoạn {stageName}"
            });

            rules.Add(new QuantityValidationRule
            {
                Type=ValidationRuleType.MaxDecrease,
                Threshold=0.20,
                ErrorCode="OutputQuantityDecreaseTooHigh",
                ErrorMessage=$"Khối lượng giảm quá nhiều cho giai đoạn {stageName}"
            });

            rules.Add(new QuantityValidationRule
            {
                Type=ValidationRuleType.MinValue,
                Threshold=0.01,
                ErrorCode="OutputQuantityTooLow",
                ErrorMessage=$"Khối lượng phải lớn hơn 0"
            });

            rules.Add(new QuantityValidationRule
            {
                Type=ValidationRuleType.MaxValue,
                Threshold=10000,
                ErrorCode="OutputQuantityTooHigh",
                ErrorMessage=$"Khối lượng quá lớn, vui lòng kiểm tra lại"
            });

            return rules;
        }

        private RetryQuantityValidationResult ValidateQuantityRule(
            QuantityValidationRule rule,
            double currentQuantity,
            double previousQuantity,
            double improvementPercentage)
        {
            var result = new RetryQuantityValidationResult { IsValid=true };

            switch (rule.Type)
            {
                case ValidationRuleType.MaxIncrease:
                    if (improvementPercentage>rule.Threshold*100)
                    {
                        result.IsValid=false;
                        result.ErrorCode=rule.ErrorCode;
                        result.ErrorParameters=new Dictionary<string, object>
                        {
                            ["CurrentQuantity"]=currentQuantity,
                            ["PreviousQuantity"]=previousQuantity,
                            ["IncreasePercentage"]=improvementPercentage,
                            ["MaxAllowed"]=rule.Threshold*100,
                            ["StageName"]="currentStage.StageName"
                        };
                    }
                    break;

                case ValidationRuleType.MaxDecrease:
                    if (improvementPercentage<-rule.Threshold*100)
                    {
                        result.IsValid=false;
                        result.ErrorCode=rule.ErrorCode;
                        result.ErrorParameters=new Dictionary<string, object>
                        {
                            ["CurrentQuantity"]=currentQuantity,
                            ["PreviousQuantity"]=previousQuantity,
                            ["DecreasePercentage"]=Math.Abs(improvementPercentage),
                            ["MaxAllowed"]=rule.Threshold*100,
                            ["StageName"]="currentStage.StageName"
                        };
                    }
                    break;

                case ValidationRuleType.MinValue:
                    if (currentQuantity<rule.Threshold)
                    {
                        result.IsValid=false;
                        result.ErrorCode=rule.ErrorCode;
                        result.ErrorParameters=new Dictionary<string, object>
                        {
                            ["CurrentQuantity"]=currentQuantity,
                            ["MinRequired"]=rule.Threshold
                        };
                    }
                    break;

                case ValidationRuleType.MaxValue:
                    if (currentQuantity>rule.Threshold)
                    {
                        result.IsValid=false;
                        result.ErrorCode=rule.ErrorCode;
                        result.ErrorParameters=new Dictionary<string, object>
                        {
                            ["CurrentQuantity"]=currentQuantity,
                            ["MaxAllowed"]=rule.Threshold
                        };
                    }
                    break;
            }

            return result;
        }

        private async Task<double> NormalizeQuantityAsync(double quantity, string unit)
        {
            return unit.ToLower() switch
            {
                "kg" => quantity,
                "g" => quantity/1000,
                "ton" => quantity*1000,
                "quintal" => quantity*100,
                "yen" => quantity*10,
                "lang" => quantity*0.0375,
                "lb" => quantity*0.453592,
                "oz" => quantity*0.0283495,
                _ => quantity
            };
        }

        private async Task<RetryQuantityValidationResult> ValidateNonFailedStageQuantityAsync(
            ProcessingStage currentStage,
            double currentQuantity,
            string currentUnit,
            double previousQuantity,
            string previousUnit)
        {
            var result = new RetryQuantityValidationResult { IsValid=true };

            var relaxedTolerance = GetToleranceForStage(currentStage.StageName)*2;

            Console.WriteLine($"DEBUG NON-FAILED VALIDATION: Stage: {currentStage.StageName}, Relaxed Tolerance: {relaxedTolerance*100}%");

            var normalizedCurrentQuantity = await NormalizeQuantityAsync(currentQuantity, currentUnit);
            var normalizedPreviousQuantity = await NormalizeQuantityAsync(previousQuantity, previousUnit);

            var changePercentage = ((normalizedCurrentQuantity-normalizedPreviousQuantity)/normalizedPreviousQuantity)*100;

            Console.WriteLine($"DEBUG NON-FAILED VALIDATION: Quantity comparison:");
            Console.WriteLine($"  - Previous: {previousQuantity} {previousUnit} (normalized: {normalizedPreviousQuantity} kg)");
            Console.WriteLine($"  - Current: {currentQuantity} {currentUnit} (normalized: {normalizedCurrentQuantity} kg)");
            Console.WriteLine($"  - Change: {changePercentage:F2}%");

            if (normalizedCurrentQuantity<=0)
            {
                result.IsValid=false;
                result.ErrorCode="OutputQuantityTooLow";
                result.ErrorParameters=new Dictionary<string, object>
                {
                    ["CurrentQuantity"]=currentQuantity,
                    ["MinRequired"]=0.01
                };
                return result;
            }

            if (normalizedCurrentQuantity>10000)
            {
                result.IsValid=false;
                result.ErrorCode="OutputQuantityTooHigh";
                result.ErrorParameters=new Dictionary<string, object>
                {
                    ["CurrentQuantity"]=currentQuantity,
                    ["MaxAllowed"]=10000
                };
                return result;
            }

            if (changePercentage>relaxedTolerance*100)
            {
                result.IsValid=false;
                result.ErrorCode="OutputQuantityIncreaseTooHigh";
                result.ErrorParameters=new Dictionary<string, object>
                {
                    ["CurrentQuantity"]=currentQuantity,
                    ["PreviousQuantity"]=previousQuantity,
                    ["IncreasePercentage"]=changePercentage,
                    ["MaxAllowed"]=relaxedTolerance*100,
                    ["StageName"]=currentStage.StageName,
                    ["Note"]="Stage không bị fail - validation nhẹ nhàng"
                };
                return result;
            }

            if (changePercentage<-relaxedTolerance*100)
            {
                result.IsValid=false;
                result.ErrorCode="OutputQuantityDecreaseTooHigh";
                result.ErrorParameters=new Dictionary<string, object>
                {
                    ["CurrentQuantity"]=currentQuantity,
                    ["PreviousQuantity"]=previousQuantity,
                    ["DecreasePercentage"]=Math.Abs(changePercentage),
                    ["MaxAllowed"]=relaxedTolerance*100,
                    ["StageName"]=currentStage.StageName,
                    ["Note"]="Stage không bị fail - validation nhẹ nhàng"
                };
                return result;
            }

            return result;
        }

        private string GetSuggestionForStage(string stageName, string errorCode)
        {
            return (stageName.ToLower(), errorCode) switch
            {
                ("thu hoạch", "OutputQuantityIncreaseTooHigh") =>
                    "Kiểm tra lại quy trình thu hoạch và đảm bảo tính nhất quán",
                ("thu hoạch", "OutputQuantityDecreaseTooHigh") =>
                    "Có thể do thời tiết hoặc chất lượng cà phê, hãy ghi chú lý do",
                ("phơi", "OutputQuantityIncreaseTooHigh") =>
                    "Kiểm tra lại thời gian phơi và điều kiện thời tiết",
                ("phơi", "OutputQuantityDecreaseTooHigh") =>
                    "Có thể do mưa hoặc độ ẩm cao, hãy ghi chú điều kiện thời tiết",
                ("xay vỏ", "OutputQuantityIncreaseTooHigh") =>
                    "Kiểm tra lại cài đặt máy xay và quy trình",
                ("xay vỏ", "OutputQuantityDecreaseTooHigh") =>
                    "Có thể do hao hụt trong quá trình xay, hãy kiểm tra máy",
                ("rang", "OutputQuantityIncreaseTooHigh") =>
                    "Kiểm tra lại nhiệt độ và thời gian rang",
                ("rang", "OutputQuantityDecreaseTooHigh") =>
                    "Có thể do hao hụt trong quá trình rang, hãy kiểm tra nhiệt độ",
                ("đóng gói", "OutputQuantityIncreaseTooHigh") =>
                    "Kiểm tra lại quy trình đóng gói và cân đo",
                ("đóng gói", "OutputQuantityDecreaseTooHigh") =>
                    "Có thể do hao hụt trong đóng gói, hãy kiểm tra quy trình",
                _ => "Vui lòng kiểm tra lại quy trình và đảm bảo tính chính xác"
            };
        }

        private enum ValidationRuleType
        {
            MaxIncrease,
            MaxDecrease,
            MinValue,
            MaxValue
        }

        private class QuantityValidationRule
        {
            public ValidationRuleType Type { get; set; }
            public double Threshold { get; set; }
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
        }

        private async Task<RetryQuantityValidationResult> ValidateAgainstCropProgressAsync(
            Guid batchId,
            double outputQuantity,
            string outputUnit)
        {
            var result = new RetryQuantityValidationResult { IsValid=true };

            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch?.CropSeasonId==null)
                {
                    return result;
                }

                var cropSeasonDetails = await _unitOfWork.CropSeasonDetailRepository.GetAllAsync(
                    csd => csd.CropSeasonId==batch.CropSeasonId&&!csd.IsDeleted,
                    q => q.OrderByDescending(csd => csd.CreatedAt)
                );

                var latestCropSeasonDetail = cropSeasonDetails.FirstOrDefault();
                if (latestCropSeasonDetail==null||!latestCropSeasonDetail.ActualYield.HasValue)
                {
                    return result;
                }

                var normalizedOutputQuantity = await NormalizeQuantityAsync(outputQuantity, outputUnit);
                var normalizedCropQuantity = await NormalizeQuantityAsync(
                    latestCropSeasonDetail.ActualYield.Value,
                    "kg"
                );

                Console.WriteLine($"DEBUG CROP VALIDATION: Output: {outputQuantity} {outputUnit} (normalized: {normalizedOutputQuantity} kg)");
                Console.WriteLine($"DEBUG CROP VALIDATION: Crop ActualYield: {latestCropSeasonDetail.ActualYield} kg (normalized: {normalizedCropQuantity} kg)");

                if (normalizedOutputQuantity>normalizedCropQuantity)
                {
                    result.IsValid=false;
                    result.ErrorCode="OutputQuantityExceedsCropProgress";
                    result.ErrorParameters=new Dictionary<string, object>
                    {
                        ["OutputQuantity"]=outputQuantity,
                        ["OutputUnit"]=outputUnit,
                        ["CropQuantity"]=latestCropSeasonDetail.ActualYield.Value,
                        ["CropUnit"]="kg",
                        ["CropSeasonDetailId"]=latestCropSeasonDetail.DetailId.ToString()
                    };
                    result.Suggestion="Khối lượng xử lý không được vượt quá khối lượng thu hoạch thực tế. Vui lòng kiểm tra lại.";
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG CROP VALIDATION ERROR: {ex.Message}");
                return result;
            }
        }

        public async Task<IServiceResult> GetAvailableBatchesForProgressAsync(Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                List<ProcessingBatch> availableBatches;

                if (isAdmin)
                {
                    availableBatches=await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                        predicate: b => !b.IsDeleted&&
                                       (b.Status==ProcessingStatus.NotStarted.ToString()||
                                        b.Status==ProcessingStatus.InProgress.ToString()),
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
                    var manager = await _unitOfWork.BusinessManagerRepository
                        .GetByIdAsync(m => m.UserId==userId&&!m.IsDeleted);

                    if (manager==null)
                        return CreateValidationError("BusinessManagerNotFound", new Dictionary<string, object>
                        {
                            ["UserId"]=userId.ToString()
                        });

                    var managerId = manager.ManagerId;

                    availableBatches=await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                        predicate: b => !b.IsDeleted&&
                                       (b.Status==ProcessingStatus.NotStarted.ToString()||
                                        b.Status==ProcessingStatus.InProgress.ToString())&&
                                       b.CropSeason!=null&&
                                       b.CropSeason.Commitment!=null&&
                                       b.CropSeason.Commitment.ApprovedBy==managerId,
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
                    var farmer = await _unitOfWork.FarmerRepository
                        .GetByIdAsync(f => f.UserId==userId&&!f.IsDeleted);

                    if (farmer==null)
                        return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                        {
                            ["UserId"]=userId.ToString()
                        });

                    availableBatches=await _unitOfWork.ProcessingBatchRepository.GetAllAsync(
                        predicate: b => !b.IsDeleted&&
                                       b.FarmerId==farmer.FarmerId&&
                                       (b.Status==ProcessingStatus.NotStarted.ToString()||
                                        b.Status==ProcessingStatus.InProgress.ToString()),
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
                        ["UserId"]=userId.ToString()
                    });
                }

                var result = new List<AvailableBatchForProgressDto>();
                foreach (var batch in availableBatches)
                {
                    var finalProgress = batch.ProcessingBatchProgresses
                        .Where(p => p.OutputQuantity.HasValue&&p.OutputQuantity.Value>0)
                        .OrderByDescending(p => p.StepIndex)
                        .FirstOrDefault();
                    var finalOutputQuantity = finalProgress?.OutputQuantity??0;

                    var remainingQuantity = batch.InputQuantity-finalOutputQuantity;

                    if (remainingQuantity>0)
                    {
                        result.Add(new AvailableBatchForProgressDto
                        {
                            BatchId=batch.BatchId,
                            BatchCode=batch.BatchCode,
                            SystemBatchCode=batch.SystemBatchCode,
                            Status=batch.Status,
                            CreatedAt=batch.CreatedAt??DateTime.MinValue,

                            CoffeeTypeId=batch.CoffeeTypeId,
                            CoffeeTypeName=batch.CoffeeType?.TypeName??"N/A",
                            CropSeasonId=batch.CropSeasonId,
                            CropSeasonName=batch.CropSeason?.SeasonName??"N/A",
                            MethodId=batch.MethodId,
                            MethodName=batch.Method?.Name??"N/A",
                            FarmerId=batch.FarmerId,
                            FarmerName=batch.Farmer?.User?.Name??"N/A",

                            TotalInputQuantity=batch.InputQuantity,
                            TotalProcessedQuantity=finalOutputQuantity,
                            RemainingQuantity=remainingQuantity,
                            InputUnit=batch.InputUnit,

                            TotalProgresses=batch.ProcessingBatchProgresses.Count,
                            LastProgressDate=batch.ProcessingBatchProgresses
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
                if (!isAdmin)
                {
                    if (isManager)
                    {
                        var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(m => m.UserId==userId&&!m.IsDeleted);
                        if (manager==null)
                        {
                            return CreateValidationError("BusinessManagerNotFound");
                        }

                        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(b => b.BatchId==batchId&&!b.IsDeleted);
                        if (batch==null)
                        {
                            return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                            {
                                ["BatchId"]=batchId.ToString()
                            });
                        }

                        var commitment = await _unitOfWork.FarmingCommitmentRepository.GetByIdAsync(c => c.CommitmentId==batch.CropSeason.CommitmentId&&!c.IsDeleted);
                        if (commitment?.ApprovedBy!=manager.ManagerId)
                        {
                            return CreateValidationError("NoPermissionToAccessBatch", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString(),
                                ["BatchId"]=batchId.ToString()
                            });
                        }
                    }
                    else
                    {
                        var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(f => f.UserId==userId&&!f.IsDeleted);
                        if (farmer==null)
                        {
                            return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString()
                            });
                        }

                        var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(b => b.BatchId==batchId&&b.FarmerId==farmer.FarmerId&&!b.IsDeleted);
                        if (batch==null)
                        {
                            return CreateValidationError("BatchNotFoundOrNoPermission", new Dictionary<string, object>
                            {
                                ["UserId"]=userId.ToString(),
                                ["BatchId"]=batchId.ToString()
                            });
                        }
                    }
                }

                var processingBatch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    predicate: b => b.BatchId==batchId&&!b.IsDeleted,
                    include: q => q
                        .Include(b => b.Method)
                        .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted).OrderBy(p => p.StepIndex))
                        .Include(b => b.ProcessingBatchProgresses).ThenInclude(p => p.Stage),
                    asNoTracking: false
                );

                if (processingBatch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (processingBatch.Status!=ProcessingStatus.InProgress.ToString()&&
                    processingBatch.Status!=ProcessingStatus.NotStarted.ToString())
                {
                    return CreateValidationError("BatchNotInProgressableState", new Dictionary<string, object>
                    {
                        ["CurrentStatus"]=processingBatch.Status
                    });
                }

                var currentStepIndex = processingBatch.ProcessingBatchProgresses.Any()
                    ? processingBatch.ProcessingBatchProgresses.Max(p => p.StepIndex)
                    : 0;

                var nextStepIndex = currentStepIndex+1;

                var nextStage = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(
                    predicate: s => s.MethodId==processingBatch.MethodId&&s.OrderIndex==nextStepIndex&&!s.IsDeleted
                );

                if (nextStage==null)
                {
                    return CreateValidationError("NextStepInfoNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString(),
                        ["NextStepIndex"]=nextStepIndex
                    });
                }

                var newProgress = new ProcessingBatchProgress
                {
                    ProgressId=Guid.NewGuid(),
                    BatchId=batchId,
                    StepIndex=nextStepIndex,
                    StageId=nextStage.StageId,
                    StageDescription=nextStage.Description,
                    ProgressDate=DateOnly.FromDateTime(DateTime.UtcNow),
                    UpdatedBy=userId,
                    CreatedAt=DateTime.UtcNow,
                    UpdatedAt=DateTime.UtcNow,
                    IsDeleted=false
                };

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(newProgress);

                if (processingBatch.Status==ProcessingStatus.NotStarted.ToString())
                {
                    processingBatch.Status=ProcessingStatus.InProgress.ToString();
                    processingBatch.UpdatedAt=DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(processingBatch);
                }

                var totalStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    predicate: s => s.MethodId==processingBatch.MethodId&&!s.IsDeleted
                );

                if (nextStepIndex>=totalStages.Count())
                {
                    var evaluation = new ProcessingBatchEvaluation
                    {
                        EvaluationId=Guid.NewGuid(),
                        EvaluationCode=await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year),
                        BatchId=batchId,
                        EvaluationResult="Temporary",
                        Comments="Đánh giá tự động sau khi hoàn thành tất cả các bước.",
                        EvaluatedAt=DateTime.UtcNow,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    };

                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(evaluation);

                    processingBatch.Status=ProcessingStatus.AwaitingEvaluation.ToString();
                    processingBatch.UpdatedAt=DateTime.UtcNow;
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

        private async Task<object?> GetFailureInfoForBatch(Guid batchId)
        {
            try
            {
                Console.WriteLine($"DEBUG: Getting failure info for batch: {batchId}");

                var latestEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId==batchId&&!e.IsDeleted,
                    q => q.OrderByDescending(e => e.CreatedAt)
                );

                var evaluation = latestEvaluation.FirstOrDefault();
                if (evaluation==null)
                {
                    Console.WriteLine($"DEBUG: No evaluation found for batch: {batchId}");
                    return null;
                }

                if (evaluation.EvaluationResult!="Fail")
                {
                    Console.WriteLine($"DEBUG: Latest evaluation is not Fail. Result: {evaluation.EvaluationResult}");
                    return null;
                }

                Console.WriteLine($"DEBUG: Found Fail evaluation. Comments: {evaluation.Comments}");

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                {
                    Console.WriteLine($"DEBUG: Batch not found: {batchId}");
                    return null;
                }

                var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex)
                );

                var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex)
                );

                var lastProgress = progresses.LastOrDefault();
                var stageToRetry = lastProgress!=null ? stages.FirstOrDefault(s => s.StageId==lastProgress.StageId) : null;

                var failureInfo = new
                {
                    BatchId = batchId,
                    EvaluationId = evaluation.EvaluationId,
                    FailedAt = evaluation.CreatedAt,
                    Comments = evaluation.Comments,
                    FailedStageId = stageToRetry?.StageId,
                    FailedStageName = stageToRetry?.StageName,
                    FailedOrderIndex = stageToRetry?.OrderIndex,
                    LastStepIndex = lastProgress?.StepIndex??0,
                    CompletedStages = progresses.Select(p => new
                    {
                        StageId = p.StageId,
                        StageName = stages.FirstOrDefault(s => s.StageId==p.StageId)?.StageName,
                        OrderIndex = stages.FirstOrDefault(s => s.StageId==p.StageId)?.OrderIndex,
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

        public async Task<IServiceResult> CreateWithMediaAndWasteAsync(Guid batchId, ProcessingBatchProgressCreateRequest input, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                Console.WriteLine($"🔍 Service: Starting create with media and waste for batchId: {batchId}");
                Console.WriteLine($"🔍 Service: Input Wastes count: {input.Wastes?.Count??0}");
                Console.WriteLine($"🔍 Service: Input WasteType: {input.WasteType}");
                Console.WriteLine($"🔍 Service: Input WasteQuantity: {input.WasteQuantity}");
                Console.WriteLine($"🔍 Service: Input WasteUnit: {input.WasteUnit}");
                Console.WriteLine($"🔍 Service: Input WasteNote: {input.WasteNote}");
                Console.WriteLine($"🔍 Service: Input WasteRecordedAt: {input.WasteRecordedAt}");

                var hasOutputQuantity = input.OutputQuantity.HasValue&&input.OutputQuantity.Value>0;
                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))||
                                   (input.Wastes?.Any()==true);

                if (hasOutputQuantity)
                {
                    var outputQuantityValidationResult = await ValidateOutputQuantityBeforeCreateProgress(batchId, input);
                    if (outputQuantityValidationResult.Status!=Const.SUCCESS_READ_CODE)
                    {
                        return outputQuantityValidationResult;
                    }
                }

                if (hasWasteData)
                {
                    var wasteValidationResult = await ValidateWasteBeforeCreateProgress(batchId, input);
                    if (wasteValidationResult.Status!=Const.SUCCESS_READ_CODE)
                    {
                        return wasteValidationResult;
                    }
                }

                var parameters = await ParseParametersFromRequest(input);

                var progressDto = new ProcessingBatchProgressCreateDto
                {
                    StageId=input.StageId,
                    ProgressDate=input.ProgressDate,
                    OutputQuantity=input.OutputQuantity,
                    OutputUnit=input.OutputUnit,
                    PhotoUrl=null,
                    VideoUrl=null,
                    Parameters=parameters.Any() ? parameters : null
                };

                var progressResult = await CreateAsync(batchId, progressDto, userId, isAdmin, isManager);
                if (progressResult.Status!=Const.SUCCESS_CREATE_CODE)
                {
                    return progressResult;
                }

                var progressId = (Guid)progressResult.Data;

                var createdWastes = new List<ProcessingWasteViewAllDto>();
                Console.WriteLine($"🔍 Service: Input Wastes count: {input.Wastes?.Count??0}");

                if (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))
                {
                    Console.WriteLine($"🔍 Service: Creating waste from individual fields - Type: {input.WasteType}, Quantity: {input.WasteQuantity}, Unit: {input.WasteUnit}");
                    var wasteDto = new ProcessingWasteCreateDto
                    {
                        WasteType=input.WasteType,
                        Quantity=input.WasteQuantity.Value,
                        Unit=input.WasteUnit,
                        Note=input.WasteNote,
                        RecordedAt=input.WasteRecordedAt??DateTime.UtcNow
                    };
                    var wasteList = new List<ProcessingWasteCreateDto> { wasteDto };
                    createdWastes=await CreateWastesForProgress(wasteList, progressId, userId, isAdmin);
                    Console.WriteLine($"🔍 Service: Created waste from individual fields, count: {createdWastes.Count}");
                }
                else if (input.Wastes?.Any()==true)
                {
                    Console.WriteLine($"🔍 Service: About to create wastes from array for progressId: {progressId}");
                    createdWastes=await CreateWastesForProgress(input.Wastes, progressId, userId, isAdmin);
                    Console.WriteLine($"🔍 Service: Created wastes from array, count: {createdWastes.Count}");
                }

                var responseParameters = new List<ProcessingParameterViewAllDto>();
                if (parameters.Any())
                {
                    responseParameters=parameters.Select(p => new ProcessingParameterViewAllDto
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=progressId,
                        ParameterName=p.ParameterName,
                        ParameterValue=p.ParameterValue,
                        Unit=p.Unit,
                        RecordedAt=p.RecordedAt
                    }).ToList();
                }

                var response = new ProcessingBatchProgressMediaResponse
                {
                    Message=progressResult.Message,
                    ProgressId=progressId,
                    PhotoUrl=null,
                    VideoUrl=null,
                    MediaCount=0,
                    AllPhotoUrls=new List<string>(),
                    AllVideoUrls=new List<string>(),
                    Parameters=responseParameters,
                    Wastes=createdWastes
                };

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, response);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi tạo progress với waste: {ex.Message}");
            }
        }

        private async Task<List<ProcessingParameterInProgressDto>> ParseParametersFromRequest(ProcessingBatchProgressCreateRequest request)
        {
            var parameters = new List<ProcessingParameterInProgressDto>();

            if (!string.IsNullOrEmpty(request.ParameterName))
            {
                parameters.Add(new ProcessingParameterInProgressDto
                {
                    ParameterName=request.ParameterName,
                    ParameterValue=request.ParameterValue,
                    Unit=request.Unit,
                    RecordedAt=request.RecordedAt
                });
            }

            if (!string.IsNullOrEmpty(request.ParametersJson))
            {
                try
                {
                    var multipleParams = System.Text.Json.JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(request.ParametersJson);
                    if (multipleParams!=null)
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

        private async Task<List<ProcessingWasteViewAllDto>> CreateWastesForProgress(List<ProcessingWasteCreateDto> wasteDtos, Guid progressId, Guid userId, bool isAdmin)
        {
            Console.WriteLine($"🔍 CreateWastesForProgress: Starting with {wasteDtos.Count} wastes");

            var createdWastes = new List<ProcessingWasteViewAllDto>();

            foreach (var wasteDto in wasteDtos)
            {
                Console.WriteLine($"🔍 CreateWastesForProgress: Processing waste - Type: {wasteDto.WasteType}, Quantity: {wasteDto.Quantity}, Unit: {wasteDto.Unit}");
                wasteDto.ProgressId=progressId;

                var wasteEntity = new ProcessingBatchWaste
                {
                    WasteId=Guid.NewGuid(),
                    WasteCode=await _codeGenerator.GenerateProcessingWasteCodeAsync(),
                    ProgressId=wasteDto.ProgressId,
                    WasteType=wasteDto.WasteType,
                    Quantity=wasteDto.Quantity,
                    Unit=wasteDto.Unit,
                    Note=wasteDto.Note,
                    RecordedAt=wasteDto.RecordedAt??DateTime.UtcNow,
                    RecordedBy=userId,
                    CreatedAt=DateTime.UtcNow,
                    UpdatedAt=DateTime.UtcNow,
                    IsDeleted=false,
                    IsDisposed=false
                };

                await _unitOfWork.ProcessingWasteRepository.CreateAsync(wasteEntity);

                var wasteViewDto = new ProcessingWasteViewAllDto
                {
                    WasteId=wasteEntity.WasteId,
                    WasteCode=wasteEntity.WasteCode,
                    ProgressId=wasteEntity.ProgressId,
                    WasteType=wasteEntity.WasteType,
                    Quantity=wasteEntity.Quantity??0,
                    Unit=wasteEntity.Unit,
                    Note=wasteEntity.Note,
                    RecordedAt=wasteEntity.RecordedAt.HasValue ? DateOnly.FromDateTime(wasteEntity.RecordedAt.Value) : null,
                    RecordedBy=wasteEntity.RecordedBy?.ToString()??"",
                    IsDisposed=wasteEntity.IsDisposed??false,
                    DisposedAt=wasteEntity.DisposedAt,
                    CreatedAt=wasteEntity.CreatedAt,
                    UpdatedAt=wasteEntity.UpdatedAt
                };

                createdWastes.Add(wasteViewDto);
            }

            Console.WriteLine($"🔍 CreateWastesForProgress: About to commit {createdWastes.Count} wastes to database");
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"🔍 CreateWastesForProgress: Successfully committed wastes to database");
            return createdWastes;
        }

        public async Task<IServiceResult> AdvanceWithMediaAndWasteAsync(Guid batchId, AdvanceProcessingBatchProgressRequest input, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                Console.WriteLine($"🔍 ADVANCE SERVICE: Starting advance for batchId: {batchId}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input Wastes count: {input.Wastes?.Count??0}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteType: {input.WasteType}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteQuantity: {input.WasteQuantity}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteUnit: {input.WasteUnit}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteNote: {input.WasteNote}");
                Console.WriteLine($"🔍 ADVANCE SERVICE: Input WasteRecordedAt: {input.WasteRecordedAt}");

                var existingProgresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderBy(p => p.StepIndex))).ToList();

                var dateValidationResult = await ValidateProgressDate(batchId, input.ProgressDate, existingProgresses);
                if (dateValidationResult.Status!=Const.SUCCESS_READ_CODE)
                {
                    return dateValidationResult;
                }

                var hasOutputQuantity = input.OutputQuantity.HasValue&&input.OutputQuantity.Value>0;
                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))||
                                   (input.Wastes?.Any()==true);

                if (hasOutputQuantity)
                {
                    var outputQuantityValidationResult = await ValidateOutputQuantityBeforeAdvanceProgress(batchId, input, userId, isAdmin, isManager);
                    if (outputQuantityValidationResult.Status!=Const.SUCCESS_READ_CODE)
                    {
                        return outputQuantityValidationResult;
                    }
                }

                if (hasWasteData)
                {
                    var wasteValidationResult = await ValidateWasteBeforeAdvanceProgress(batchId, input);
                    if (wasteValidationResult.Status!=Const.SUCCESS_READ_CODE)
                    {
                        return wasteValidationResult;
                    }
                }

                var parameters = await ParseParametersFromRequest(input);

                var advanceDto = new AdvanceProcessingBatchProgressDto
                {
                    ProgressDate=input.ProgressDate,
                    OutputQuantity=input.OutputQuantity,
                    OutputUnit=input.OutputUnit,
                    PhotoUrl=null,
                    VideoUrl=null,
                    Parameters=parameters.Any() ? parameters : null,
                    StageId=input.StageId,
                    CurrentStageId=input.CurrentStageId,
                    StageDescription=input.StageDescription
                };

                var advanceResult = await AdvanceProgressByBatchIdAsync(batchId, advanceDto, userId, isAdmin, isManager);
                if (advanceResult.Status!=Const.SUCCESS_CREATE_CODE&&advanceResult.Status!=Const.SUCCESS_UPDATE_CODE)
                {
                    return advanceResult;
                }

                var latestProgressResult = await GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);
                var actualProgressId = Guid.Empty;

                if (latestProgressResult.Status==Const.SUCCESS_READ_CODE&&latestProgressResult.Data is List<ProcessingBatchProgressViewAllDto> progressesList)
                {
                    var latestProgressDto = progressesList.LastOrDefault();
                    if (latestProgressDto!=null)
                    {
                        actualProgressId=latestProgressDto.ProgressId;
                    }
                }

                var createdWastes = new List<ProcessingWasteViewAllDto>();
                Console.WriteLine($"🔍 ADVANCE SERVICE: About to process wastes for progressId: {actualProgressId}");

                if (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))
                {
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Creating waste from individual fields - Type: {input.WasteType}, Quantity: {input.WasteQuantity}, Unit: {input.WasteUnit}");
                    var wasteDto = new ProcessingWasteCreateDto
                    {
                        WasteType=input.WasteType,
                        Quantity=input.WasteQuantity.Value,
                        Unit=input.WasteUnit,
                        Note=input.WasteNote,
                        RecordedAt=input.WasteRecordedAt??DateTime.UtcNow
                    };
                    var wasteList = new List<ProcessingWasteCreateDto> { wasteDto };
                    createdWastes=await CreateWastesForProgress(wasteList, actualProgressId, userId, isAdmin);
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Created waste from individual fields, count: {createdWastes.Count}");
                }
                else if (input.Wastes?.Any()==true)
                {
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Creating wastes from array, count: {input.Wastes.Count}");
                    createdWastes=await CreateWastesForProgress(input.Wastes, actualProgressId, userId, isAdmin);
                    Console.WriteLine($"🔍 ADVANCE SERVICE: Created wastes from array, count: {createdWastes.Count}");
                }
                else
                {
                    Console.WriteLine($"🔍 ADVANCE SERVICE: No valid waste data found to process");
                }

                var responseParameters = new List<ProcessingParameterViewAllDto>();
                if (parameters.Any())
                {
                    responseParameters=parameters.Select(p => new ProcessingParameterViewAllDto
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=actualProgressId,
                        ParameterName=p.ParameterName,
                        ParameterValue=p.ParameterValue,
                        Unit=p.Unit,
                        RecordedAt=p.RecordedAt
                    }).ToList();
                }

                Console.WriteLine($"🔍 ADVANCE SERVICE: Creating response with {createdWastes.Count} wastes");
                var response = new ProcessingBatchProgressMediaResponse
                {
                    Message=advanceResult.Message,
                    ProgressId=actualProgressId,
                    PhotoUrl=null,
                    VideoUrl=null,
                    MediaCount=0,
                    AllPhotoUrls=new List<string>(),
                    AllVideoUrls=new List<string>(),
                    Parameters=responseParameters,
                    Wastes=createdWastes
                };

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, response);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi khi advance progress với waste: {ex.Message}");
            }
        }

        private async Task<List<ProcessingParameterInProgressDto>> ParseParametersFromRequest(AdvanceProcessingBatchProgressRequest request)
        {
            var parameters = new List<ProcessingParameterInProgressDto>();

            if (!string.IsNullOrEmpty(request.ParameterName))
            {
                parameters.Add(new ProcessingParameterInProgressDto
                {
                    ParameterName=request.ParameterName,
                    ParameterValue=request.ParameterValue,
                    Unit=request.Unit,
                    RecordedAt=request.RecordedAt
                });
            }

            if (!string.IsNullOrEmpty(request.ParametersJson))
            {
                try
                {
                    var multipleParams = System.Text.Json.JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(request.ParametersJson);
                    if (multipleParams!=null)
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

        private async Task<IServiceResult> ValidateWasteBeforeCreateProgress(Guid batchId, ProcessingBatchProgressCreateRequest input)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Starting pre-validation for batchId: {batchId}");

                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))||
                                   (input.Wastes?.Any()==true);

                if (!hasWasteData)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: No waste data found, skipping validation");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (!input.OutputQuantity.HasValue||input.OutputQuantity.Value<=0)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Current input has no valid output quantity");
                    return CreateMissingInfoError("OutputQuantity", "OutputQuantity");
                }

                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit??"kg";

                var maxAllowedWasteFromBatch = batch.InputQuantity-currentOutputQuantity;

                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Batch-based waste validation:");
                Console.WriteLine($"  - Batch input quantity: {batch.InputQuantity} {batch.InputUnit}");
                Console.WriteLine($"  - Current output quantity: {currentOutputQuantity} {currentOutputUnit}");
                Console.WriteLine($"  - Max allowed waste from batch: {maxAllowedWasteFromBatch} {batch.InputUnit}");

                double totalWasteQuantity = 0;

                if (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))
                {
                    var wasteQuantityInKg = ConvertToKg(input.WasteQuantity.Value, input.WasteUnit);
                    totalWasteQuantity+=wasteQuantityInKg;
                    Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Individual waste: {input.WasteQuantity.Value} {input.WasteUnit} = {wasteQuantityInKg} kg");
                }

                if (input.Wastes?.Any()==true)
                {
                    foreach (var wasteDto in input.Wastes)
                    {
                        var wasteQuantityInKg = ConvertToKg(wasteDto.Quantity, wasteDto.Unit);
                        totalWasteQuantity+=wasteQuantityInKg;
                        Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Array waste {wasteDto.WasteType}: {wasteDto.Quantity} {wasteDto.Unit} = {wasteQuantityInKg} kg");
                    }
                }

                var batchInputQuantityInKg = ConvertToKg(batch.InputQuantity, batch.InputUnit);
                var currentQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);
                var maxAllowedWasteInKg = batchInputQuantityInKg-currentQuantityInKg;

                Console.WriteLine($"🔍 ValidateWasteBeforeCreateProgress: Total waste: {totalWasteQuantity} kg, Max allowed from batch: {maxAllowedWasteInKg} kg");

                if (totalWasteQuantity>maxAllowedWasteInKg)
                {
                    double maxAllowedWithTolerance;
                    if (maxAllowedWasteInKg<=0)
                    {
                        maxAllowedWithTolerance=1.0;
                    }
                    else
                    {
                        var tolerance = 0.10;
                        maxAllowedWithTolerance=Math.Max(maxAllowedWasteInKg*(1+tolerance), maxAllowedWasteInKg+5.0);
                    }

                    if (totalWasteQuantity>maxAllowedWithTolerance)
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

        private double ConvertToKg(double quantity, string unit)
        {
            var unitLower = unit?.Trim().ToLower()??"kg";

            return unitLower switch
            {
                "kg" => quantity,
                "g" => quantity/1000.0,
                "tấn" => quantity*1000.0,
                "ton" => quantity*1000.0,
                "lít" => quantity,
                "ml" => quantity/1000.0,
                "bao" => quantity*50.0,
                "thùng" => quantity*25.0,
                _ => quantity
            };
        }

        private IServiceResult CreateWasteQuantityError(double totalWaste, double maxAllowed,
            double previousOutput, string previousUnit, double currentOutput, string currentUnit)
        {
            var parameters = new Dictionary<string, object>
            {
                ["TotalWaste"]=totalWaste,
                ["MaxAllowed"]=maxAllowed,
                ["PreviousOutput"]=previousOutput,
                ["PreviousUnit"]=previousUnit,
                ["CurrentOutput"]=currentOutput,
                //["CurrentUnit"]=currentOutputUnit,
                ["WasteExceeded"]=totalWaste-maxAllowed
            };

            return CreateValidationError("WasteQuantityExceeded", parameters);
        }

        private IServiceResult CreateWasteQuantityExceedsBatchLimitError(double totalWaste, double maxAllowed,
            double batchInput, string batchInputUnit, double currentOutput, string currentOutputUnit)
        {
            var parameters = new Dictionary<string, object>
            {
                ["TotalWaste"]=totalWaste,
                ["MaxAllowed"]=maxAllowed,
                ["BatchInputQuantity"]=batchInput,
                ["BatchInputUnit"]=batchInputUnit,
                ["CurrentOutput"]=currentOutput,
                ["CurrentOutputUnit"]=currentOutputUnit,
                ["WasteExceeded"]=totalWaste-maxAllowed
            };

            return CreateFieldValidationError("WasteQuantityExceedsBatchLimit", "WasteQuantity", parameters);
        }

        private IServiceResult CreateLogicQuantityError(double previousOutput, string previousUnit,
            double currentOutput, string currentUnit)
        {
            var parameters = new Dictionary<string, object>
            {
                ["PreviousOutput"]=previousOutput,
                ["PreviousUnit"]=previousUnit,
                ["CurrentOutput"]=currentOutput,
                ["CurrentUnit"]=currentUnit,
                ["Difference"]=currentOutput-previousOutput
            };

            return CreateValidationError("InvalidOutputQuantityLogic", parameters);
        }

        private IServiceResult CreateMissingInfoError(string fieldName, string fieldType = "OutputQuantity")
        {
            var parameters = new Dictionary<string, object>
            {
                ["FieldName"]=fieldName,
                ["FieldType"]=fieldType,
                ["Required"]=true
            };

            return CreateFieldValidationError("MissingRequiredField", fieldName, parameters);
        }

        private IServiceResult CreateNotFoundError(string entityName, string entityId = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["EntityName"]=entityName,
                ["EntityId"]=entityId
            };

            return CreateValidationError("EntityNotFound", parameters);
        }

        private IServiceResult CreatePermissionError(string action, string resource, string userId = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["Action"]=action,
                ["Resource"]=resource,
                ["UserId"]=userId
            };

            return CreateValidationError("PermissionDenied", parameters);
        }

        private IServiceResult CreateInvalidStatusError(string currentStatus, string expectedStatus, string entityName = "Batch")
        {
            var parameters = new Dictionary<string, object>
            {
                ["CurrentStatus"]=currentStatus,
                ["ExpectedStatus"]=expectedStatus,
                ["EntityName"]=entityName
            };

            return CreateValidationError("InvalidStatus", parameters);
        }

        private async Task<IServiceResult> ValidateWasteBeforeAdvanceProgress(Guid batchId, AdvanceProcessingBatchProgressRequest input)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Starting pre-validation for batchId: {batchId}");

                var hasWasteData = (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))||
                                   (input.Wastes?.Any()==true);

                if (!hasWasteData)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: No waste data found, skipping validation");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                var currentProgresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex)
                );

                var currentProgress = currentProgresses.FirstOrDefault();
                if (currentProgress==null)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: No current progress found, this is first step");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                var previousProgresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&p.StepIndex<currentProgress.StepIndex&&!p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex)
                );

                var previousProgress = previousProgresses.FirstOrDefault();
                if (previousProgress==null)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: No previous progress found");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                if (!previousProgress.OutputQuantity.HasValue||previousProgress.OutputQuantity.Value<=0)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Previous progress has no valid output quantity");
                    return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG);
                }

                var previousOutputQuantity = previousProgress.OutputQuantity.Value;
                var previousOutputUnit = previousProgress.OutputUnit??"kg";

                if (!input.OutputQuantity.HasValue||input.OutputQuantity.Value<=0)
                {
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Current input has no valid output quantity");
                    return CreateMissingInfoError("OutputQuantity", "OutputQuantity");
                }

                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit??"kg";

                var maxAllowedWasteFromPrevious = previousOutputQuantity-currentOutputQuantity;

                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Previous progress-based waste validation:");
                Console.WriteLine($"  - Previous output quantity: {previousOutputQuantity} {previousOutputUnit}");
                Console.WriteLine($"  - Current output quantity: {currentOutputQuantity} {currentOutputUnit}");
                Console.WriteLine($"  - Max allowed waste from previous: {maxAllowedWasteFromPrevious} {previousOutputUnit}");

                if (maxAllowedWasteFromPrevious<=0)
                {
                    if (maxAllowedWasteFromPrevious<0)
                    {
                        return CreateValidationError("InvalidOutputQuantityIncrease", new Dictionary<string, object>
                        {
                            ["PreviousOutput"]=previousOutputQuantity,
                            ["PreviousUnit"]=previousOutputUnit,
                            ["CurrentOutput"]=currentOutputQuantity,
                            ["CurrentUnit"]=currentOutputUnit
                        });
                    }
                    else
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            ["previousOutput"]=previousOutputQuantity,
                            ["previousUnit"]=previousOutputUnit,
                            ["currentOutput"]=currentOutputQuantity,
                            ["currentUnit"]=currentOutputUnit
                        };
                        return CreateValidationError("InvalidOutputQuantityEqual", parameters);
                    }
                }

                double totalWasteQuantity = 0;

                if (!string.IsNullOrEmpty(input.WasteType)&&input.WasteQuantity>0&&!string.IsNullOrEmpty(input.WasteUnit))
                {
                    var wasteQuantityInKg = ConvertToKg(input.WasteQuantity.Value, input.WasteUnit);
                    totalWasteQuantity+=wasteQuantityInKg;
                    Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Individual waste: {input.WasteQuantity.Value} {input.WasteUnit} = {wasteQuantityInKg} kg");
                }

                if (input.Wastes?.Any()==true)
                {
                    foreach (var wasteDto in input.Wastes)
                    {
                        var wasteQuantityInKg = ConvertToKg(wasteDto.Quantity, wasteDto.Unit);
                        totalWasteQuantity+=wasteQuantityInKg;
                        Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Array waste {wasteDto.WasteType}: {wasteDto.Quantity} {wasteDto.Unit} = {wasteQuantityInKg} kg");
                    }
                }

                var previousQuantityInKg = ConvertToKg(previousOutputQuantity, previousOutputUnit);
                var currentQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);
                var maxAllowedWasteInKg = previousQuantityInKg-currentQuantityInKg;

                Console.WriteLine($"🔍 ValidateWasteBeforeAdvanceProgress: Total waste: {totalWasteQuantity} kg, Max allowed from previous: {maxAllowedWasteInKg} kg");

                if (totalWasteQuantity>maxAllowedWasteInKg)
                {
                    double maxAllowedWithTolerance;
                    if (maxAllowedWasteInKg<=0)
                    {
                        maxAllowedWithTolerance=1.0;
                    }
                    else
                    {
                        var tolerance = 0.10;
                        maxAllowedWithTolerance=Math.Max(maxAllowedWasteInKg*(1+tolerance), maxAllowedWasteInKg+5.0);
                    }

                    if (totalWasteQuantity>maxAllowedWithTolerance)
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

        private async Task<IServiceResult> ValidateProgressDate(Guid batchId, DateOnly? progressDate, List<ProcessingBatchProgress> existingProgresses)
        {
            try
            {
                if (!progressDate.HasValue)
                {
                    return CreateFieldValidationError("ProgressDate", "ProgressDate");
                }

                var selectedDate = progressDate.Value;
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                if (selectedDate>today)
                {
                    return CreateValidationError("ProgressDateInFuture", new Dictionary<string, object>
                    {
                        ["ProgressDate"]=selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                        ["Today"]=today.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                    });
                }

                var minDatePast = today.AddDays(-365);
                if (selectedDate<minDatePast)
                {
                    return CreateValidationError("ProgressDateTooPast", new Dictionary<string, object>
                    {
                        ["ProgressDate"]=selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                        ["MinDate"]=minDatePast.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                    });
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (!existingProgresses.Any())
                {
                    var allCropProgress = await _unitOfWork.CropProgressRepository.GetAllAsync(
                        p => p.CropSeasonDetail.CropSeasonId==batch.CropSeasonId&&
                             p.CropSeasonDetail.CommitmentDetail.PlanDetail.CoffeeTypeId==batch.CoffeeTypeId&&
                             p.ProgressDate.HasValue&&
                             !p.IsDeleted&&
                             !p.CropSeasonDetail.IsDeleted,
                        include: q => q.Include(p => p.CropSeasonDetail)
                                      .ThenInclude(d => d.CommitmentDetail)
                                      .ThenInclude(cd => cd.PlanDetail)
                                      .Include(p => p.Stage)
                    );

                    if (allCropProgress.Any())
                    {
                        var lastProgressDate = allCropProgress.Max(p => p.ProgressDate.Value);

                        Console.WriteLine($"🔍 DEBUG FirstProgressDateAfterHarvest:");
                        Console.WriteLine($"  - Selected Date: {selectedDate} ({selectedDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                        Console.WriteLine($"  - Last Progress Date: {lastProgressDate} ({lastProgressDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                        Console.WriteLine($"  - Comparison: {selectedDate} < {lastProgressDate} = {selectedDate<lastProgressDate}");

                        if (selectedDate<lastProgressDate)
                        {
                            return CreateValidationError("FirstProgressDateAfterHarvest", new Dictionary<string, object>
                            {
                                ["ProgressDate"]=selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                ["HarvestDate"]=lastProgressDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                ["MinDate"]=lastProgressDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                            });
                        }
                    }
                    else
                    {
                        var cropSeasonDetail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                            d => d.CropSeasonId==batch.CropSeasonId&&
                                 d.CommitmentDetail.PlanDetail.CoffeeTypeId==batch.CoffeeTypeId&&
                                 !d.IsDeleted,
                            include: q => q.Include(d => d.CommitmentDetail).ThenInclude(cd => cd.PlanDetail)
                        );

                        if (cropSeasonDetail?.ExpectedHarvestEnd.HasValue==true)
                        {
                            var harvestEndDate = cropSeasonDetail.ExpectedHarvestEnd.Value;

                            Console.WriteLine($"🔍 DEBUG FirstProgressDateAfterHarvest (Fallback):");
                            Console.WriteLine($"  - Selected Date: {selectedDate} ({selectedDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                            Console.WriteLine($"  - Expected Harvest End: {harvestEndDate} ({harvestEndDate.ToDateTime(TimeOnly.MinValue):dd/MM/yyyy})");
                            Console.WriteLine($"  - Comparison: {selectedDate} < {harvestEndDate} = {selectedDate<harvestEndDate}");

                            if (selectedDate<harvestEndDate)
                            {
                                return CreateValidationError("FirstProgressDateAfterHarvest", new Dictionary<string, object>
                                {
                                    ["ProgressDate"]=selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                    ["HarvestDate"]=harvestEndDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                                    ["MinDate"]=harvestEndDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
                                });
                            }
                        }
                    }
                }
                else
                {
                    var latestProgress = existingProgresses.OrderByDescending(p => p.StepIndex).First();

                    if (selectedDate<latestProgress.ProgressDate.Value)
                    {
                        return CreateValidationError("ProgressDateAfterPrevious", new Dictionary<string, object>
                        {
                            ["ProgressDate"]=selectedDate.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                            ["PreviousProgressDate"]=latestProgress.ProgressDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"),
                            ["MinDate"]=latestProgress.ProgressDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy")
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

        private async Task<IServiceResult> ValidateOutputQuantityBeforeCreateProgress(Guid batchId, ProcessingBatchProgressCreateRequest input)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Starting validation for batchId: {batchId}");

                if (!input.OutputQuantity.HasValue||input.OutputQuantity.Value<=0)
                {
                    Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Invalid output quantity: {input.OutputQuantity}");
                    return CreateFieldValidationError("OutputQuantity", "OutputQuantity");
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit??"kg";
                var batchInputQuantity = batch.InputQuantity;
                var batchInputUnit = batch.InputUnit;

                var batchInputQuantityInKg = ConvertToKg(batchInputQuantity, batchInputUnit);
                var currentOutputQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);

                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeCreateProgress: Quantity validation:");
                Console.WriteLine($"  - Batch input: {batchInputQuantity} {batchInputUnit} = {batchInputQuantityInKg} kg");
                Console.WriteLine($"  - Current output: {currentOutputQuantity} {currentOutputUnit} = {currentOutputQuantityInKg} kg");

                if (currentOutputQuantityInKg>batchInputQuantityInKg)
                {
                    return CreateFieldValidationError("OutputQuantityExceedsInput", "OutputQuantity", new Dictionary<string, object>
                    {

                        ["InputQuantity"]=batchInputQuantity,
                        ["InputUnit"]=batchInputUnit,
                        ["OutputQuantity"]=currentOutputQuantity,
                        ["OutputUnit"]=currentOutputUnit
                    });
                }

                if (Math.Abs(currentOutputQuantityInKg-batchInputQuantityInKg)<0.01)
                {
                    return CreateFieldValidationError("OutputQuantityEqualNotAllowed", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["PreviousOutputQuantity"]=batchInputQuantity,
                        ["PreviousOutputUnit"]=batchInputUnit,
                        ["CurrentOutputQuantity"]=currentOutputQuantity,
                        ["CurrentOutputUnit"]=currentOutputUnit
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

        private async Task<IServiceResult> ValidateOutputQuantityBeforeAdvanceProgress(Guid batchId, AdvanceProcessingBatchProgressRequest input, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Starting validation for batchId: {batchId}");

                if (!input.OutputQuantity.HasValue||input.OutputQuantity.Value<=0)
                {
                    Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Invalid output quantity: {input.OutputQuantity}");
                    return CreateFieldValidationError("OutputQuantity", "OutputQuantity");
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                var latestProgressResult = await GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);
                if (latestProgressResult.Status!=Const.SUCCESS_READ_CODE||latestProgressResult.Data is not List<ProcessingBatchProgressViewAllDto> progressesList||!progressesList.Any())
                {
                    return CreateValidationError("NoPreviousProgress", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                var latestProgress = progressesList.Last();
                var previousOutputQuantity = latestProgress.OutputQuantity??0;
                var previousOutputUnit = latestProgress.OutputUnit??"kg";
                var currentOutputQuantity = input.OutputQuantity.Value;
                var currentOutputUnit = input.OutputUnit??"kg";

                var previousOutputQuantityInKg = ConvertToKg(previousOutputQuantity, previousOutputUnit);
                var currentOutputQuantityInKg = ConvertToKg(currentOutputQuantity, currentOutputUnit);

                Console.WriteLine($"🔍 ValidateOutputQuantityBeforeAdvanceProgress: Quantity validation:");
                Console.WriteLine($"  - Previous output: {previousOutputQuantity} {previousOutputUnit} = {previousOutputQuantityInKg} kg");
                Console.WriteLine($"  - Current output: {currentOutputQuantity} {currentOutputUnit} = {currentOutputQuantityInKg} kg");

                if (currentOutputQuantityInKg>previousOutputQuantityInKg)
                {
                    return CreateFieldValidationError("OutputQuantityExceedsPrevious", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["PreviousOutputQuantity"]=previousOutputQuantity,
                        ["PreviousOutputUnit"]=previousOutputUnit,
                        ["CurrentOutputQuantity"]=currentOutputQuantity,
                        ["CurrentOutputUnit"]=currentOutputUnit
                    });
                }

                if (Math.Abs(currentOutputQuantityInKg-previousOutputQuantityInKg)<0.01)
                {
                    return CreateFieldValidationError("OutputQuantityEqualNotAllowed", "OutputQuantity", new Dictionary<string, object>
                    {
                        ["PreviousOutputQuantity"]=previousOutputQuantity,
                        ["PreviousOutputUnit"]=previousOutputUnit,
                        ["CurrentOutputQuantity"]=currentOutputQuantity,
                        ["CurrentOutputUnit"]=currentOutputUnit
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

        private async Task<RetryQuantityValidationResult> ValidateRetryQuantityWithWasteAsync(
            ProcessingStage currentStage,
            double retryQuantity,
            string retryUnit,
            double previousQuantity,
            string previousUnit,
            Guid batchId)
        {
            var result = new RetryQuantityValidationResult { IsValid=true };

            Console.WriteLine($"DEBUG WASTE VALIDATION: Validating retry quantity for stage {currentStage.StageName}");
            Console.WriteLine($"DEBUG WASTE VALIDATION: Retry quantity: {retryQuantity} {retryUnit}");
            Console.WriteLine($"DEBUG WASTE VALIDATION: Previous quantity: {previousQuantity} {previousUnit}");

            var retryQuantityInKg = await NormalizeQuantityAsync(retryQuantity, retryUnit);
            var previousQuantityInKg = await NormalizeQuantityAsync(previousQuantity, previousUnit);

            if (retryQuantityInKg<=0)
            {
                result.IsValid=false;
                result.ErrorCode="RetryQuantityMustBePositive";
                result.ErrorParameters=new Dictionary<string, object>
                {
                    ["RetryQuantity"]=retryQuantity,
                    ["RetryUnit"]=retryUnit,
                    ["StageName"]=currentStage.StageName,
                    ["Note"]="Khối lượng retry phải lớn hơn 0"
                };
                return result;
            }

            var retryQuantityInKgForValidation = await NormalizeQuantityAsync(retryQuantity, retryUnit);
            var previousQuantityInKgForValidation = await NormalizeQuantityAsync(previousQuantity, previousUnit);
            var waste = Math.Max(0, previousQuantityInKgForValidation-retryQuantityInKgForValidation);
            var wastePercentage = previousQuantityInKgForValidation>0 ? (waste/previousQuantityInKgForValidation)*100 : 0;

            Console.WriteLine($"DEBUG AUTO WASTE VALIDATION: Stage: {currentStage.StageName}");
            Console.WriteLine($"DEBUG AUTO WASTE VALIDATION: Previous quantity: {previousQuantityInKgForValidation} kg");
            Console.WriteLine($"DEBUG AUTO WASTE VALIDATION: Retry quantity: {retryQuantityInKgForValidation} kg");
            Console.WriteLine($"DEBUG AUTO WASTE VALIDATION: Auto-calculated waste: {waste} kg ({wastePercentage:F1}%)");

            var maxWastePercentage = ProcessingHelper.GetMaxWastePercentageForStage(currentStage.StageName);

            if (wastePercentage>maxWastePercentage)
            {
                result.IsValid=false;
                result.ErrorCode="WastePercentageTooHigh";
                result.ErrorParameters=new Dictionary<string, object>
                {
                    ["WastePercentage"]=wastePercentage,
                    ["MaxAllowedPercentage"]=maxWastePercentage,
                    ["WasteQuantity"]=waste,
                    ["StageName"]=currentStage.StageName,
                    ["Note"]=$"Tỷ lệ waste quá cao cho stage {currentStage.StageName}. Có thể do lỗi đo lường."
                };
                return result;
            }

            if (waste<0)
            {
                var improvementPercentage = Math.Abs(wastePercentage);
                Console.WriteLine($"DEBUG WASTE VALIDATION: Improvement detected: {improvementPercentage:F1}%");

                if (improvementPercentage>20)
                {
                    result.IsValid=false;
                    result.ErrorCode="ImprovementTooHigh";
                    result.ErrorParameters=new Dictionary<string, object>
                    {
                        ["ImprovementPercentage"]=improvementPercentage,
                        ["MaxAllowedImprovement"]=20,
                        ["StageName"]=currentStage.StageName,
                        ["Note"]=$"Cải thiện quá cao ({improvementPercentage:F1}%). Có thể do lỗi đo lường."
                    };
                    return result;
                }
            }

            Console.WriteLine($"DEBUG WASTE VALIDATION: Validation passed - Waste: {waste} kg ({wastePercentage:F1}%)");
            return result;
        }

        private async Task<double> GetFinalOutputBeforeRetryAsync(Guid batchId)
        {
            var allProgresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                p => p.BatchId==batchId&&!p.IsDeleted,
                q => q.OrderByDescending(p => p.StepIndex)
            );

            var finalProgress = allProgresses.FirstOrDefault();
            if (finalProgress?.OutputQuantity==null)
            {
                return 0;
            }

            return await NormalizeQuantityAsync(finalProgress.OutputQuantity.Value, finalProgress.OutputUnit??"kg");
        }

        private ServiceResult CreateFieldValidationError(string errorKey, string fieldName, Dictionary<string, object> parameters = null)
        {
            var errorData = new
            {
                ErrorKey = errorKey,
                FieldName = fieldName,
                Parameters = parameters??new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                ErrorType = "FieldValidationError"
            };

            string message = GetFieldValidationErrorMessage(errorKey, fieldName, parameters);

            return new ServiceResult(Const.ERROR_VALIDATION_CODE, message, errorData);
        }

        private string GetFieldValidationErrorMessage(string errorKey, string fieldName, Dictionary<string, object> parameters)
        {
            return errorKey;
        }

        public async Task<IServiceResult> UpdateNextStagesAsync(
            Guid batchId,
            ProcessingBatchProgressCreateDto input,
            Guid userId,
            bool isAdmin,
            bool isManager)
        {
            try
            {
                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Starting update for batchId: {batchId}, userId: {userId}");

                if (batchId==Guid.Empty)
                    return CreateValidationError("InvalidBatchId", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });

                var farmer = (await _unitOfWork.FarmerRepository.GetAllAsync(f => f.UserId==userId&&!f.IsDeleted)).FirstOrDefault();
                if (farmer==null)
                {
                    return CreateValidationError("FarmerNotFound", new Dictionary<string, object>
                    {
                        ["UserId"]=userId.ToString()
                    });
                }

                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null||batch.IsDeleted)
                {
                    return CreateValidationError("BatchNotFound", new Dictionary<string, object>
                    {
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (batch.FarmerId!=farmer.FarmerId)
                {
                    return CreateValidationError("NoPermissionToUpdateBatch", new Dictionary<string, object>
                    {
                        ["UserId"]=userId.ToString(),
                        ["BatchId"]=batchId.ToString()
                    });
                }

                if (batch.Status!="InProgress"&&batch.Status!="AwaitingEvaluation")
                {
                    return CreateValidationError("CannotUpdateProgressBatchNotInProgress", new Dictionary<string, object>
                    {
                        ["CurrentStatus"]=batch.Status
                    });
                }

                var stages = (await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex))).ToList();

                if (stages.Count==0)
                {
                    return CreateValidationError("NoStagesForMethod", new Dictionary<string, object>
                    {
                        ["MethodId"]=batch.MethodId.ToString()
                    });
                }

                var progresses = (await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex))).ToList();

                var latestProgress = progresses.FirstOrDefault();
                int nextStepIndex = latestProgress!=null ? latestProgress.StepIndex+1 : 1;

                var nextStage = stages.FirstOrDefault(s => s.OrderIndex==nextStepIndex);
                if (nextStage==null)
                {
                    return CreateValidationError("NoNextStageAvailable", new Dictionary<string, object>
                    {
                        ["CurrentStepIndex"]=nextStepIndex-1,
                        ["MaxStepIndex"]=stages.Max(s => s.OrderIndex)
                    });
                }

                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Next stage: {nextStage.StageName} (OrderIndex: {nextStage.OrderIndex})");

                if (!input.OutputQuantity.HasValue||input.OutputQuantity.Value<=0)
                {
                    return CreateValidationError("OutputQuantityMustBePositive", new Dictionary<string, object>
                    {
                        ["OutputQuantity"]=input.OutputQuantity?.ToString()??"null",
                        ["MinValue"]=0
                    });
                }

                var existingStepIndex = await _unitOfWork.ProcessingBatchProgressRepository.AnyAsync(
                    p => p.BatchId==batchId&&p.StepIndex==nextStepIndex&&!p.IsDeleted
                );
                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: StepIndex {nextStepIndex} already exists: {existingStepIndex}");

                if (existingStepIndex)
                {
                    return CreateValidationError("StepIndexAlreadyExists", new Dictionary<string, object>
                    {
                        ["StepIndex"]=nextStepIndex
                    });
                }

                var progress = new ProcessingBatchProgress
                {
                    ProgressId=Guid.NewGuid(),
                    BatchId=batchId,
                    StepIndex=nextStepIndex,
                    StageId=nextStage.StageId,
                    StageDescription=$"Làm lại (Retry) - {nextStage.StageName}",
                    ProgressDate=input.ProgressDate,
                    OutputQuantity=input.OutputQuantity,
                    OutputUnit=string.IsNullOrWhiteSpace(input.OutputUnit) ? "kg" : input.OutputUnit,
                    PhotoUrl=input.PhotoUrl,
                    VideoUrl=input.VideoUrl,
                    CreatedAt=DateTime.UtcNow,
                    UpdatedAt=DateTime.UtcNow,
                    UpdatedBy=farmer.FarmerId,
                    IsDeleted=false,
                    ProcessingParameters=new List<ProcessingParameter>()
                };

                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Creating progress with:");
                Console.WriteLine($"  - ProgressId: {progress.ProgressId}");
                Console.WriteLine($"  - BatchId: {progress.BatchId}");
                Console.WriteLine($"  - StepIndex: {progress.StepIndex}");
                Console.WriteLine($"  - StageId: {progress.StageId}");
                Console.WriteLine($"  - StageName: {nextStage.StageName}");
                Console.WriteLine($"  - UpdatedBy: {progress.UpdatedBy}");
                Console.WriteLine($"  - OutputQuantity: {progress.OutputQuantity}");
                Console.WriteLine($"  - OutputUnit: {progress.OutputUnit}");

                await _unitOfWork.ProcessingBatchProgressRepository.CreateAsync(progress);

                if (input.Parameters?.Any()==true)
                {
                    Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Creating {input.Parameters.Count} parameters");
                    var parametersToCreate = input.Parameters.Select(p => new ProcessingParameter
                    {
                        ParameterId=Guid.NewGuid(),
                        ProgressId=progress.ProgressId,
                        ParameterName=p.ParameterName,
                        ParameterValue=p.ParameterValue,
                        Unit=p.Unit,
                        RecordedAt=p.RecordedAt??DateTime.UtcNow,
                        CreatedAt=DateTime.UtcNow,
                        UpdatedAt=DateTime.UtcNow,
                        IsDeleted=false
                    }).ToList();

                    foreach (var param in parametersToCreate)
                    {
                        Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Creating parameter: {param.ParameterName} = {param.ParameterValue} {param.Unit}");
                        await _unitOfWork.ProcessingParameterRepository.CreateAsync(param);
                    }
                    Console.WriteLine($"DEBUG UPDATE NEXT STAGES: All parameters created successfully");
                }
                else
                {
                    Console.WriteLine($"DEBUG UPDATE NEXT STAGES: No parameters to create");
                }

                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: About to save changes...");
                try
                {
                    var saveResult = await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"DEBUG UPDATE NEXT STAGES: SaveChangesAsync returned: {saveResult}");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"DEBUG UPDATE NEXT STAGES: SaveChangesAsync failed: {saveEx.Message}");
                    Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Inner exception: {saveEx.InnerException?.Message}");
                    throw;
                }

                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Successfully created progress for next stage");
                Console.WriteLine($"DEBUG UPDATE NEXT STAGES: Progress created with ID: {progress.ProgressId}");

                var successMessage = $"Đã cập nhật progress cho giai đoạn {nextStage.StageName} (Bước {nextStepIndex})";

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, successMessage, progress.ProgressId);

            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> GetBatchInfoBeforeRetryAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    b => b.BatchId==batchId&&!b.IsDeleted,
                    include: q => q
                        .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedAt))
                        .Include(b => b.Method)
                        .ThenInclude(m => m.ProcessingStages.Where(s => !s.IsDeleted).OrderBy(s => s.OrderIndex))
                );

                if (batch==null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô chế biến.");

                if (!isAdmin&&!isManager)
                {
                    var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                        f => f.UserId==userId&&!f.IsDeleted
                    );
                    if (farmer==null||batch.FarmerId!=farmer.FarmerId)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có quyền truy cập lô này.");
                }

                var lastProgress = batch.ProcessingBatchProgresses.FirstOrDefault();
                if (lastProgress==null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy tiến trình nào cho lô này.");

                var currentStage = batch.Method.ProcessingStages.FirstOrDefault(s => s.StageId==lastProgress.StageId);
                if (currentStage==null)
                {
                    currentStage=batch.Method.ProcessingStages.FirstOrDefault();
                    if (currentStage==null)
                    {
                        var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                            s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                            q => q.OrderBy(s => s.OrderIndex)
                        );
                        currentStage=stages.FirstOrDefault();

                        if (currentStage==null)
                            return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông tin giai đoạn nào.");
                    }
                }

                var finalOutputBeforeRetry = lastProgress.OutputQuantity??0;
                var finalOutputUnit = lastProgress.OutputUnit??"kg";
                var maxWastePercentage = ProcessingHelper.GetMaxWastePercentageForStage(currentStage.StageName);

                var retryInfo = new
                {
                    finalOutputBeforeRetry = finalOutputBeforeRetry,
                    finalOutputUnit = finalOutputUnit,
                    maxAllowedRetryQuantity = finalOutputBeforeRetry,
                    calculatedWaste = 0,
                    wastePercentage = 0,
                    maxWastePercentage = maxWastePercentage,
                    isValid = true,
                    errorMessage = (string?)null
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy thông tin retry thành công", retryInfo);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Đã xảy ra lỗi hệ thống: {ex.Message}");
            }
        }

        public async Task<IServiceResult> CheckBatchCanCreateProgressAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    b => b.BatchId==batchId&&!b.IsDeleted,
                    include: q => q
                        .Include(b => b.Method)
                        .Include(b => b.CropSeason)
                        .Include(b => b.CoffeeType)
                        .Include(b => b.Farmer).ThenInclude(f => f.User)
                        .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted))
                );

                if (batch==null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy lô chế biến.");

                if (!isAdmin&&!isManager)
                {
                    var farmer = await _unitOfWork.FarmerRepository
                        .GetByIdAsync(f => f.UserId==userId&&!f.IsDeleted);

                    if (farmer==null)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy thông tin nông hộ.");

                    if (batch.FarmerId!=farmer.FarmerId)
                        return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Bạn không có quyền truy cập lô chế biến này.");
                }

                var finalProgress = batch.ProcessingBatchProgresses
                    .Where(p => p.OutputQuantity.HasValue&&p.OutputQuantity.Value>0)
                    .OrderByDescending(p => p.StepIndex)
                    .FirstOrDefault();
                var finalOutputQuantity = finalProgress?.OutputQuantity??0;

                var remainingQuantity = batch.InputQuantity-finalOutputQuantity;
                var canCreateProgress = remainingQuantity>0;

                var result = new
                {
                    BatchId = batch.BatchId,
                    BatchCode = batch.BatchCode,
                    Status = batch.Status,
                    CanCreateProgress = canCreateProgress,
                    TotalInputQuantity = batch.InputQuantity,
                    TotalProcessedQuantity = finalOutputQuantity,
                    RemainingQuantity = remainingQuantity,
                    InputUnit = batch.InputUnit,
                    Message = canCreateProgress
                        ? $"Có thể tạo tiến độ. Còn lại {remainingQuantity} {batch.InputUnit}"
                        : $"Không thể tạo tiến độ. Đã chế biến hết khối lượng."
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Kiểm tra batch thành công", result);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Đã xảy ra lỗi hệ thống: {ex.Message}");
            }
        }

        public async Task<IServiceResult> DebugAdvanceAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager)
        {
            try
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch==null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Batch không tồn tại.");

                var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId==batch.MethodId&&!s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex)
                );

                var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId==batchId&&!p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex)
                );

                var latestProgress = progresses.FirstOrDefault();

                var debugInfo = new
                {
                    message = "Debug advance info",
                    batchId,
                    userId,
                    batchStatus = batch.Status,
                    roles = new
                    {
                        isAdmin,
                        isManager,
                        isFarmer = !isAdmin&&!isManager
                    },
                    stages = stages.Select(s => new
                    {
                        stageId = s.StageId,
                        stageName = s.StageName,
                        orderIndex = s.OrderIndex
                    }).ToList(),
                    totalStages = stages.Count(),
                    progresses = progresses.Select(p => new
                    {
                        progressId = p.ProgressId,
                        stepIndex = p.StepIndex,
                        stageId = p.StageId,
                        progressDate = p.ProgressDate
                    }).ToList(),
                    totalProgresses = progresses.Count(),
                    latestProgress = latestProgress!=null ? new
                    {
                        progressId = latestProgress.ProgressId,
                        stepIndex = latestProgress.StepIndex,
                        stageId = latestProgress.StageId
                    } : null,
                    note = "Chỉ Farmer mới được phép advance progress"
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Debug info retrieved successfully", debugInfo);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi debug: {ex.Message}");
            }
        }

        // 🔧 MỚI: Xử lý hoàn thành retry - chuyển batch status và tạo evaluation mới
        private async Task HandleRetryCompletionAsync(Guid batchId, Guid farmerId)
        {
            try
            {
                Console.WriteLine($"DEBUG HANDLE RETRY COMPLETION: Starting for batch {batchId}");

                // 1. Cập nhật batch status sang AwaitingEvaluation
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch!=null)
                {
                    batch.Status="AwaitingEvaluation";
                    batch.UpdatedAt=DateTime.UtcNow;

                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    Console.WriteLine($"DEBUG HANDLE RETRY COMPLETION: Updated batch status to AwaitingEvaluation");
                }

                // 2. Tạo evaluation mới cho expert đánh giá lại
                var newEvaluation = new ProcessingBatchEvaluation
                {
                    EvaluationId=Guid.NewGuid(),
                    BatchId=batchId,
                    EvaluatedBy=null, // Sẽ được gán cho expert sau
                    EvaluatedAt=null, // Sẽ được cập nhật khi expert đánh giá
                    EvaluationResult="Pending", // Trạng thái chờ đánh giá
                    Comments=$"Đánh giá lại sau khi farmer retry stage. Batch ID: {batchId}",
                    CreatedAt=DateTime.UtcNow,
                    UpdatedAt=DateTime.UtcNow,
                    IsDeleted=false
                };

                await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(newEvaluation);
                Console.WriteLine($"DEBUG HANDLE RETRY COMPLETION: Created new evaluation {newEvaluation.EvaluationId}");

                // 3. Commit changes
                await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"DEBUG HANDLE RETRY COMPLETION: Successfully completed for batch {batchId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR HANDLE RETRY COMPLETION: {ex.Message}");
                throw;
            }
        }
    }
}