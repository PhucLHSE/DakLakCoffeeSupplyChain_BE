using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
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
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    using Microsoft.EntityFrameworkCore;
    // ...

    public class EvaluationService : IEvaluationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;

        public EvaluationService(
            IUnitOfWork unitOfWork, 
            ICodeGenerator codeGenerator,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
        }

        private ServiceResult CreateValidationError(string errorKey, Dictionary<string, object> parameters = null)
        {
            return new ServiceResult(Const.ERROR_VALIDATION_CODE, errorKey, parameters);
        }



     
        private async Task<bool> HasPermissionToAccessAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            if (isAdmin || isManager || isExpert) return true;

            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                predicate: b => b.BatchId == batchId && !b.IsDeleted,
                include: q => q.Include(b => b.Farmer)
            );
            return batch?.Farmer?.UserId == userId;
        }

        // ================== CREATE ==================
        public async Task<IServiceResult> CreateAsync(EvaluationCreateDto dto, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            // Quyền tạo: theo rule chung trên (Admin/Manager/Expert hoặc Farmer của chính batch)
            var canAccess = await HasPermissionToAccessAsync(dto.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToCreateEvaluation");

            // Tồn tại batch?
            var batchExists = await _unitOfWork.ProcessingBatchRepository.AnyAsync(
                b => b.BatchId == dto.BatchId && !b.IsDeleted
            );
            if (!batchExists)
                return CreateValidationError("InvalidProcessingBatch");

                                                   // 🔧 MỚI: Chỉ validate EvaluationResult nếu không có QualityCriteriaEvaluations
              if (dto.QualityCriteriaEvaluations?.Any() != true && !string.IsNullOrEmpty(dto.EvaluationResult))
              {
                  var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary" };
                  if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
                      return CreateValidationError("InvalidEvaluationResult");
              }
                
            // 🔧 MỚI: Validate TotalScore nếu có
            if (dto.TotalScore.HasValue && (dto.TotalScore.Value < 0 || dto.TotalScore.Value > 100))
            {
                return CreateValidationError("InvalidTotalScore", new Dictionary<string, object>
                {
                    ["TotalScore"] = dto.TotalScore.Value,
                    ["MinValue"] = 0,
                    ["MaxValue"] = 100
                });
            }
            
            // 🔧 MỚI: Validate IsPassAllBatch
            if (dto.IsPassAllBatch == true && dto.TotalScore.HasValue && dto.TotalScore.Value != 100)
            {
                return CreateValidationError("InvalidPassAllBatchScore", new Dictionary<string, object>
                {
                    ["TotalScore"] = dto.TotalScore.Value,
                    ["ExpectedScore"] = 100
                });
            }

            // Kiểm tra batch status trước khi đánh giá
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId);
            if (batch == null)
                return CreateValidationError("ProcessingBatchNotFound");

            // Cho phép Expert tạo đánh giá cho batch đã hoàn thành, đang chờ đánh giá, hoặc đang xử lý
            // Cho phép Admin/Manager tạo đánh giá cho mọi trạng thái
            // Cho phép Farmer tạo đơn đánh giá khi batch đã hoàn thành
            if (isExpert)
            {
                // Expert có thể tạo đánh giá cho batch Completed, AwaitingEvaluation, hoặc InProgress
                if (batch.Status != "Completed" && batch.Status != "AwaitingEvaluation" && batch.Status != "InProgress")
                    return CreateValidationError("ExpertCanOnlyEvaluateCompletedAwaitingOrInProgress");
            }
            else if (isAdmin || isManager)
            {
                // Admin/Manager có thể tạo đánh giá cho mọi trạng thái
            }
            else
            {
                // Farmer chỉ có thể tạo đơn đánh giá khi batch đã hoàn thành
                if (batch.Status != "Completed")
                    return CreateValidationError("FarmerCanOnlyRequestEvaluationForCompleted");
            }

            var code = await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year);

            // Tạo comments chi tiết bao gồm thông tin đơn yêu cầu đánh giá và tiến trình
            var detailedComments = dto.Comments ?? "";

            // 🔧 TÍCH HỢP: Logic đánh giá chất lượng dựa theo tiêu chí từ SystemConfiguration
            if (dto.QualityCriteriaEvaluations?.Any() == true)
            {
                try
                {
                    // Tạo comment đánh giá chất lượng theo format mới
                    var qualityComment = CreateQualityEvaluationComment(dto.QualityCriteriaEvaluations, dto.ExpertNotes);

                    // Thêm comment đánh giá chất lượng vào đầu
                    detailedComments = qualityComment + "\n\n" + detailedComments;
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không dừng quá trình tạo evaluation
                    Console.WriteLine($"Lỗi đánh giá chất lượng: {ex.Message}");
                    detailedComments = $"[LỖI ĐÁNH GIÁ CHẤT LƯỢNG: {ex.Message}]\n\n" + detailedComments;
                }
            }


            
            // 🔧 CẢI THIỆN: Logic tạo comments thông minh cho evaluation
            if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) && 
                dto.ProblematicSteps?.Any() == true)
            {
                // Nếu là Fail và có problematic steps, tạo format chuẩn
                var problematicStep = dto.ProblematicSteps.First();
                
                // Tạo format đơn giản cho problematic steps
                    if (!string.IsNullOrEmpty(dto.DetailedFeedback))
                    {
                        detailedComments += $"\n\nChi tiết vấn đề: {dto.DetailedFeedback}";
                    }
                    if (dto.ProblematicSteps?.Any() == true)
                    {
                        detailedComments += $"\nTiến trình có vấn đề: {string.Join(", ", dto.ProblematicSteps)}";
                    }
                    if (!string.IsNullOrEmpty(dto.Recommendations))
                    {
                        detailedComments += $"\nKhuyến nghị: {dto.Recommendations}";
                }
            }
            else
            {
                // Tạo comments thông thường cho các trường hợp khác
                if (!string.IsNullOrEmpty(dto.DetailedFeedback))
                {
                    detailedComments += $"\n\nChi tiết vấn đề: {dto.DetailedFeedback}";
                }
                if (dto.ProblematicSteps?.Any() == true)
                {
                    detailedComments += $"\nTiến trình có vấn đề: {string.Join(", ", dto.ProblematicSteps)}";
                }
                if (!string.IsNullOrEmpty(dto.Recommendations))
                {
                    detailedComments += $"\nKhuyến nghị: {dto.Recommendations}";
                }
            }
            
            // Thêm thông tin đơn yêu cầu đánh giá nếu có
            if (!string.IsNullOrEmpty(dto.RequestReason))
            {
                detailedComments += $"\n\nLý do yêu cầu đánh giá: {dto.RequestReason}";
            }
            if (!string.IsNullOrEmpty(dto.AdditionalNotes))
            {
                detailedComments += $"\nGhi chú bổ sung: {dto.AdditionalNotes}";
            }

            // Lấy ExpertId từ UserId nếu là expert
            Guid? expertId = null;
            if (isExpert)
            {
                try
                {
                    var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                        predicate: e => e.UserId == userId && !e.IsDeleted,
                        asNoTracking: true
                    );
                    expertId = expert?.ExpertId;
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không dừng quá trình tạo evaluation
                    Console.WriteLine($"Lỗi tìm AgriculturalExpert: {ex.Message}");
                    expertId = null; // Sử dụng null nếu không tìm thấy
                }
            }

                         // 🔧 CẢI THIỆN: Logic tính điểm thông minh dựa trên tiêu chí
             // Hệ thống sẽ tự động đánh giá dựa trên quality criteria mà không cần expert chọn thủ công
             decimal? finalScore = null;
             string finalResult = dto.EvaluationResult;
            
            Console.WriteLine($"DEBUG EVALUATION CREATE: dto.IsPassAllBatch = {dto.IsPassAllBatch}, dto.TotalScore = {dto.TotalScore}");
            
            // Nếu có nút "Đạt cả batch" từ FE
            if (dto.IsPassAllBatch == true)
            {
                finalScore = 100.00m;
                finalResult = "Pass";
                // 🔧 CẢI THIỆN: Chỉ thêm comment nếu chưa có
                if (!detailedComments.Contains("Đạt cả batch") && !detailedComments.Contains("Đánh giá BATCH thành công"))
                {
                    detailedComments = "Đánh giá: Đạt cả batch (100/100 điểm)\n\n" + detailedComments;
                }
                Console.WriteLine($"DEBUG EVALUATION CREATE: IsPassAllBatch = true, finalScore = {finalScore}, finalResult = {finalResult}");
            }
            // 🔧 FIX: Xử lý trường hợp có EvaluationResult từ DTO nhưng không có TotalScore
            else if (!string.IsNullOrEmpty(dto.EvaluationResult) && !dto.TotalScore.HasValue)
            {
                if (dto.EvaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase))
                {
                    finalScore = 100.00m;
                }
                else if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
                {
                    finalScore = 0.00m;
                }
                else
                {
                    finalScore = 50.00m; // Default cho các trường hợp khác
                }
                Console.WriteLine($"DEBUG EVALUATION CREATE: Set score based on EvaluationResult: {dto.EvaluationResult}, finalScore = {finalScore}");
            }
                                      // 🔧 MỚI: Hệ thống tự động đánh giá dựa trên quality criteria (không cần expert chọn thủ công)
             else if (dto.QualityCriteriaEvaluations?.Any() == true)
             {
                 // Tự động đánh giá dựa trên quality criteria
                 var criteriaResults = QualityEvaluationHelper.AutoEvaluateCriteria(dto.QualityCriteriaEvaluations);
                 var passCount = criteriaResults.Count(c => c.IsPassed);
                 var failCount = criteriaResults.Count(c => !c.IsPassed);
                 
                 // 🔧 FIX: Tính điểm dựa trên tỷ lệ Pass/Fail
                 if (criteriaResults.Count > 0)
                 {
                     // Tính điểm theo tỷ lệ: (PassCount / TotalCount) * 100
                     finalScore = Math.Round((decimal)passCount / criteriaResults.Count * 100, 2);
                     Console.WriteLine($"DEBUG EVALUATION CREATE: Auto calculated score: {passCount}/{criteriaResults.Count} = {finalScore}");
                 }
                 else
                 {
                     finalScore = 0.00m;
                 }
                 
                                   // 🔧 MỚI: Logic đánh giá tự động dựa trên tỷ lệ Pass/Fail
                  // - Pass > Fail: Tự động Pass
                  // - Fail > Pass: Tự động Fail  
                  // - Pass = Fail (50/50): Expert quyết định thủ công
                  if (passCount > failCount)
                 {
                     // Nếu Pass > Fail: Đánh giá Pass + note cần cải thiện
                     finalResult = "Pass";
                     
                                           // Lấy danh sách các giai đoạn bị lỗi để ghi nhận (dù đã Pass)
                      var failedStages = await GetFailedStagesFromCriteriaAsync(dto.QualityCriteriaEvaluations, dto.BatchId);
                      var failedStagesText = failedStages.Any() 
                          ? $"Giai đoạn cần cải thiện thêm: {string.Join(", ", failedStages.Select(s => s.StageName))}"
                          : "";
                     
                     detailedComments = $"Đánh giá: Đạt. Tuy nhiên, cần cải thiện thêm một số vấn đề.\n{failedStagesText}\n\n" + detailedComments;
                 }
                 else if (failCount > passCount)
                 {
                     // Nếu Fail > Pass: Đánh giá Fail + lưu stages fail để farmer cập nhật
                     finalResult = "Fail";
                     
                                      // Lấy danh sách các giai đoạn bị lỗi để farmer cập nhật
                 var failedStages = await GetFailedStagesFromCriteriaAsync(dto.QualityCriteriaEvaluations, dto.BatchId);
                                   Console.WriteLine($"DEBUG EVALUATION CREATE: GetFailedStagesFromCriteriaAsync returned: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");
                 
                     // 🔧 FIX: Tạo comment chi tiết với tiêu chí fail và StageId - ĐẢM BẢO LUÔN CÓ THÔNG TIN
                     var failedCriteria = criteriaResults.Where(c => !c.IsPassed).ToList();
                     var failedCriteriaText = failedCriteria.Any() 
                         ? $"🔧 Tiêu chí không đạt: {string.Join(", ", failedCriteria.Select(c => $"{c.CriteriaName} (Giá trị: {c.ActualValue})"))}"
                         : "🔧 Tiêu chí không đạt: Không xác định";
                     
                                      var failedStagesText = failedStages.Any() 
                         ? $"🔧 Giai đoạn cần cập nhật: {string.Join(", ", failedStages.Select((stage, index) => $"StageId: {stage.StageId}"))}"
                         : "🔧 Giai đoạn cần cập nhật: Không xác định";
                     
                     // 🔧 FIX: Đảm bảo comment luôn có thông tin đầy đủ
                     detailedComments = $"Đánh giá: Không đạt.\n{failedCriteriaText}\n{failedStagesText}\n\n" + detailedComments;
                     
                     Console.WriteLine($"DEBUG EVALUATION CREATE: Created Fail comment with criteria: {failedCriteriaText}");
                     Console.WriteLine($"DEBUG EVALUATION CREATE: Created Fail comment with stages: {failedStagesText}");
                 }
                                                     else // passCount == failCount (50/50)
                   {
                       // 🔧 MỚI: Khi 50/50, để expert quyết định thủ công
                       if (!string.IsNullOrEmpty(dto.EvaluationResult))
                       {
                           // Expert đã chọn thủ công
                           finalResult = dto.EvaluationResult;
                           Console.WriteLine($"DEBUG EVALUATION CREATE: Expert manually chose {finalResult} for 50/50 case");
                           
                           // 🔧 MỚI: Nếu expert chọn Fail trong 50/50, lưu stages cần cập nhật
                           if (finalResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
                           {
                               var failedStages = await GetFailedStagesFromCriteriaAsync(dto.QualityCriteriaEvaluations, dto.BatchId);
                                                               Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 Fail case - GetFailedStagesFromCriteriaAsync returned: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");
                                
                               // 🔧 FIX: Tạo comment chi tiết với tiêu chí fail và StageId - ĐẢM BẢO LUÔN CÓ THÔNG TIN
                               var failedCriteria = criteriaResults.Where(c => !c.IsPassed).ToList();
                               var failedCriteriaText = failedCriteria.Any() 
                                   ? $"🔧 Tiêu chí không đạt: {string.Join(", ", failedCriteria.Select(c => $"{c.CriteriaName} (Giá trị: {c.ActualValue})"))}"
                                   : "🔧 Tiêu chí không đạt: Không xác định";
                               
                                                               var failedStagesText = failedStages.Any() 
                                   ? $"🔧 Giai đoạn cần cập nhật: {string.Join(", ", failedStages.Select((stage, index) => $"StageId: {stage.StageId}"))}"
                                   : "🔧 Giai đoạn cần cập nhật: Không xác định";
                               
                               detailedComments = $"Đánh giá: Không đạt. Số tiêu chí đạt và không đạt bằng nhau (50/50). Expert đã quyết định thủ công.\n{failedCriteriaText}\n{failedStagesText}\n\n" + detailedComments;
                               
                               Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 Fail - Created comment with criteria: {failedCriteriaText}");
                               Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 Fail - Created comment with stages: {failedStagesText}");
                           }
                                                       else if (finalResult.Equals("Pass", StringComparison.OrdinalIgnoreCase))
                            {
                                var failedStages = await GetFailedStagesFromCriteriaAsync(dto.QualityCriteriaEvaluations, dto.BatchId);
                                var failedStagesText = failedStages.Any() 
                                    ? $"Giai đoạn cần cải thiện thêm: {string.Join(", ", failedStages.Select(s => s.StageName))}"
                                    : "";
                               
                               detailedComments = $"Đánh giá: Đạt. Số tiêu chí đạt và không đạt bằng nhau (50/50). Expert đã quyết định thủ công.\n{failedStagesText}\n\n" + detailedComments;
                           }
                       }
                       else
                       {
                           // Nếu expert chưa chọn, để NeedsImprovement để expert quyết định sau
                           finalResult = "NeedsImprovement";
                           Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 case - waiting for expert decision");
                           
                           var failedStages = await GetFailedStagesFromCriteriaAsync(dto.QualityCriteriaEvaluations, dto.BatchId);
                                                       Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 NeedsImprovement case - GetFailedStagesFromCriteriaAsync returned: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");
                            
                           // 🔧 FIX: Tạo comment chi tiết với tiêu chí fail và StageId - ĐẢM BẢO LUÔN CÓ THÔNG TIN
                           var failedCriteria = criteriaResults.Where(c => !c.IsPassed).ToList();
                           var failedCriteriaText = failedCriteria.Any() 
                               ? $"🔧 Tiêu chí không đạt: {string.Join(", ", failedCriteria.Select(c => $"{c.CriteriaName} (Giá trị: {c.ActualValue})"))}"
                               : "🔧 Tiêu chí không đạt: Không xác định";
                           
                                                       var failedStagesText = failedStages.Any() 
                               ? $"🔧 Giai đoạn cần cải thiện: {string.Join(", ", failedStages.Select((stage, index) => $"StageId: {stage.StageId}"))}"
                               : "🔧 Giai đoạn cần cải thiện: Không xác định";
                           
                           detailedComments = $"Đánh giá: Cần cải thiện. Số tiêu chí đạt và không đạt bằng nhau (50/50). Cần expert quyết định thủ công.\n{failedCriteriaText}\n{failedStagesText}\n\n" + detailedComments;
                           
                           Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 NeedsImprovement - Created comment with criteria: {failedCriteriaText}");
                           Console.WriteLine($"DEBUG EVALUATION CREATE: 50/50 NeedsImprovement - Created comment with stages: {failedStagesText}");
                       }
                   }
                 
                 detailedComments = $"Tổng số tiêu chí: {criteriaResults.Count}, Đạt: {passCount}, Không đạt: {failCount}\n\n" + detailedComments;
                 
                 Console.WriteLine($"DEBUG EVALUATION CREATE: Auto evaluation - Pass: {passCount}, Fail: {failCount}, FinalResult: {finalResult}, FinalScore: {finalScore}");
             }
                         
            else
            {
                // Sử dụng điểm số từ DTO nếu có
                finalScore = dto.TotalScore;
                
                // 🔧 FIX: Nếu không có TotalScore từ DTO và không có quality criteria, set default score
                if (!finalScore.HasValue)
                {
                    // Nếu có EvaluationResult từ DTO, set score tương ứng
                    if (dto.EvaluationResult?.Equals("Pass", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        finalScore = 100.00m;
                    }
                    else if (dto.EvaluationResult?.Equals("Fail", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        finalScore = 0.00m;
                    }
                    else
                    {
                        finalScore = 0.00m; // Default score
                    }
                }
            }

            var entity = new ProcessingBatchEvaluation
            {
                EvaluationId = Guid.NewGuid(),
                EvaluationCode = code,
                BatchId = dto.BatchId,
                EvaluatedBy = expertId, // Lưu ExpertId thay vì UserId
                EvaluationResult = finalResult,
                TotalScore = finalScore,
                Comments = detailedComments.Trim(),
                EvaluatedAt = dto.EvaluatedAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            // 🔧 FIX: Đảm bảo TotalScore được set đúng cách
            if (finalScore.HasValue)
            {
                entity.TotalScore = finalScore.Value;
                Console.WriteLine($"DEBUG EVALUATION CREATE: Set TotalScore to entity: {entity.TotalScore}");
            }
            else
            {
                Console.WriteLine($"DEBUG EVALUATION CREATE: WARNING - finalScore is null!");
            }
            
            Console.WriteLine($"DEBUG EVALUATION CREATE: Entity TotalScore = {entity.TotalScore}, EvaluationResult = {entity.EvaluationResult}");
            Console.WriteLine($"DEBUG EVALUATION CREATE: Final calculated score = {finalScore}, Final result = {finalResult}");

            // Nếu là farmer tạo đơn đánh giá, set EvaluationResult = null
            if (!isAdmin && !isManager && !isExpert)
            {
                entity.EvaluationResult = null; // 🔧 CẢI THIỆN: Để null thống nhất
                entity.EvaluatedAt = null; // Chưa được đánh giá
            }

            // 🔧 CẢI THIỆN: Kiểm tra xem có evaluation nào cần cập nhật không
            var existingEvaluations = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => e.BatchId == dto.BatchId && !e.IsDeleted,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                asNoTracking: false // 🔧 FIX: Bật tracking để có thể update
            );
            var existingEvaluation = existingEvaluations.FirstOrDefault();
            
            Console.WriteLine($"DEBUG EVALUATION CREATE: Found {existingEvaluations.Count()} existing evaluations for batch {dto.BatchId}");
            if (existingEvaluation != null)
            {
                Console.WriteLine($"DEBUG EVALUATION CREATE: Latest existing evaluation: {existingEvaluation.EvaluationId}, CreatedAt: {existingEvaluation.CreatedAt}");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Existing evaluation state - EvaluationResult: '{existingEvaluation.EvaluationResult}', TotalScore: {existingEvaluation.TotalScore}");
            }
            
            int saved = 0;
            ProcessingBatchEvaluation trackedEvaluation = null; // 🔧 FIX: Khai báo biến ở ngoài scope
            try
            {
                if (existingEvaluation != null)
                {
                    // Cập nhật evaluation hiện có thay vì tạo mới
                    // 🔧 FIX: Lấy lại entity với tracking để đảm bảo update hoạt động
                    trackedEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                        e => e.EvaluationId == existingEvaluation.EvaluationId && !e.IsDeleted,
                        asNoTracking: false
                    );
                    
                    if (trackedEvaluation != null)
                    {
                        trackedEvaluation.EvaluatedBy = expertId;
                        trackedEvaluation.EvaluationResult = finalResult;
                        trackedEvaluation.TotalScore = finalScore;
                        trackedEvaluation.Comments = detailedComments.Trim();
                        trackedEvaluation.EvaluatedAt = dto.EvaluatedAt ?? DateTime.UtcNow;
                        trackedEvaluation.UpdatedAt = DateTime.UtcNow;
                        
                        // 🔧 FIX: Đảm bảo TotalScore được set đúng cách cho tracked entity
                        if (finalScore.HasValue)
                        {
                            trackedEvaluation.TotalScore = finalScore.Value;
                            Console.WriteLine($"DEBUG EVALUATION CREATE: Set TotalScore to tracked entity: {trackedEvaluation.TotalScore}");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG EVALUATION CREATE: WARNING - finalScore is null for tracked entity!");
                        }
                        
                        // 🔧 FIX: Đảm bảo comment được cập nhật đúng cách
                        Console.WriteLine($"DEBUG EVALUATION CREATE: Updated comments length: {trackedEvaluation.Comments?.Length ?? 0}");
                        Console.WriteLine($"DEBUG EVALUATION CREATE: Updated comments preview: {trackedEvaluation.Comments?.Substring(0, Math.Min(100, trackedEvaluation.Comments.Length))}...");
                        
                        // 🔧 FIX: Thêm debug log trước khi update
                        Console.WriteLine($"DEBUG EVALUATION CREATE: Before update - EvaluationResult: '{trackedEvaluation.EvaluationResult}', TotalScore: {trackedEvaluation.TotalScore}");
                        
                        await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(trackedEvaluation);
                        Console.WriteLine($"DEBUG EVALUATION CREATE: Updated existing evaluation: {trackedEvaluation.EvaluationId}");
                        Console.WriteLine($"DEBUG EVALUATION CREATE: After update - EvaluationResult: '{trackedEvaluation.EvaluationResult}', TotalScore: {trackedEvaluation.TotalScore}");
                    }
                    else
                    {
                        // Fallback: tạo mới nếu không lấy được entity tracked
                        await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(entity);
                        Console.WriteLine($"DEBUG EVALUATION CREATE: Created new evaluation (fallback): {entity.EvaluationId}");
                    }
                }
                else
                {
                    // Tạo evaluation mới nếu không có evaluation nào
                    await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(entity);
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Created new evaluation: {entity.EvaluationId}");
                    
                    // 🔧 FIX: Đảm bảo entity được track sau khi create
                    trackedEvaluation = entity;
                }

                // Xử lý logic workflow theo kết quả đánh giá
                var evaluationToProcess = trackedEvaluation ?? existingEvaluation ?? entity;
                
                if (finalResult == null)
                {
                    // Nếu farmer tạo đơn đánh giá hoặc evaluation tự động, chuyển batch sang AwaitingEvaluation
                    if (batch.Status == "Completed")
                    {
                        batch.Status = "AwaitingEvaluation";
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }
                }
                                 else if (finalResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
                 {
                     // 🔧 MỚI: Xử lý logic retry khi evaluation fail
                     if (batch.Status == "Completed" || batch.Status == "AwaitingEvaluation")
                     {
                         // Chuyển batch về trạng thái InProgress để farmer cập nhật các stages fail
                         batch.Status = "InProgress";
                         batch.UpdatedAt = DateTime.UtcNow;
                         await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                         
                         // 🔧 MỚI: Lưu thông tin stages cần cập nhật vào comment để farmer biết
                         Console.WriteLine($"DEBUG EVALUATION CREATE: Batch status changed to InProgress for Fail evaluation");
                         
                         // Gửi notification cho Farmer (chỉ gửi 1 lần)
                         try
                         {
                             var batchWithFarmer = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                                 predicate: b => b.BatchId == dto.BatchId,
                                 include: b => b.Include(b => b.Farmer)
                             );
                             
                             if (batchWithFarmer?.Farmer?.UserId != null)
                             {
                                 await _notificationService.NotifyEvaluationFailedAsync(
                                     dto.BatchId, 
                                     batchWithFarmer.Farmer.UserId, 
                                     detailedComments
                                 );
                             }
                         }
                         catch (Exception ex)
                         {
                             // Log lỗi notification nhưng không ảnh hưởng đến việc tạo evaluation
                             Console.WriteLine($"Lỗi gửi notification: {ex.Message}");
                         }
                     }
                 }
                                 else if (finalResult.Equals("Pass", StringComparison.OrdinalIgnoreCase))
                 {
                     bool statusChanged = false;
                     
                     // Nếu đánh giá Pass và batch đang AwaitingEvaluation, chuyển sang Completed
                     if (batch.Status == "AwaitingEvaluation")
                     {
                         batch.Status = "Completed";
                         batch.UpdatedAt = DateTime.UtcNow;
                         await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                         statusChanged = true;
                     }
                     // Nếu đánh giá Pass và batch đang InProgress, chuyển sang Completed
                     else if (batch.Status == "InProgress")
                     {
                         batch.Status = "Completed";
                         batch.UpdatedAt = DateTime.UtcNow;
                         await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                         statusChanged = true;
                     }
                     
                     // �� XÓA: Không cần xóa SystemConfiguration nữa
                     
                     Console.WriteLine($"DEBUG EVALUATION CREATE: Pass evaluation - Batch status changed to Completed: {statusChanged}");
                 }

                // 🔧 MỚI: Log trước khi save để debug
                Console.WriteLine($"DEBUG EVALUATION CREATE: About to save changes...");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity to save - EvaluationId: {entity.EvaluationId}, EvaluationResult: '{entity.EvaluationResult}', TotalScore: {entity.TotalScore}");
                if (existingEvaluation != null)
                {
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Existing evaluation to update - EvaluationId: {existingEvaluation.EvaluationId}");
                }
                Console.WriteLine($"DEBUG EVALUATION CREATE: Batch status before save: {batch.Status}");

                // 🔧 FIX: Thêm debug log để kiểm tra entity state
                if (trackedEvaluation != null)
                {
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Tracked evaluation before save - EvaluationResult: '{trackedEvaluation.EvaluationResult}', TotalScore: {trackedEvaluation.TotalScore}");
                }

                saved = await _unitOfWork.SaveChangesAsync();
                Console.WriteLine($"DEBUG EVALUATION CREATE: SaveChangesAsync result: {saved}");
                
                // 🔧 FIX: Nếu SaveChangesAsync trả về 0, thử force save
                if (saved == 0)
                {
                    Console.WriteLine($"DEBUG EVALUATION CREATE: SaveChangesAsync returned 0, trying to force save...");
                    
                    // Thử save lại với explicit tracking
                    if (trackedEvaluation != null)
                    {
                        await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(trackedEvaluation);
                        saved = await _unitOfWork.SaveChangesAsync();
                        Console.WriteLine($"DEBUG EVALUATION CREATE: Force save result: {saved}");
                    }
                }
                
                // 🔧 MỚI: Thêm logging chi tiết để debug
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity state before return - EvaluationId: {entity.EvaluationId}");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity state before return - EvaluationResult: '{entity.EvaluationResult}'");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity state before return - TotalScore: {entity.TotalScore}");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity state before return - Comments: '{entity.Comments}'");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity state before return - EvaluatedBy: {entity.EvaluatedBy}");
                Console.WriteLine($"DEBUG EVALUATION CREATE: Entity state before return - EvaluatedAt: {entity.EvaluatedAt}");
                
                if (existingEvaluation != null)
                {
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Existing evaluation state - EvaluationId: {existingEvaluation.EvaluationId}");
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Existing evaluation state - EvaluationResult: '{existingEvaluation.EvaluationResult}'");
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Existing evaluation state - TotalScore: {existingEvaluation.TotalScore}");
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Existing evaluation state - Comments: '{existingEvaluation.Comments}'");
                }
                
                // 🔧 MỚI: Kiểm tra batch state
                var batchAfterSave = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    b => b.BatchId == dto.BatchId && !b.IsDeleted
                );
                if (batchAfterSave != null)
                {
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Batch state after save - Status: '{batchAfterSave.Status}'");
                    Console.WriteLine($"DEBUG EVALUATION CREATE: Batch state after save - UpdatedAt: {batchAfterSave.UpdatedAt}");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"Lỗi tạo evaluation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw để controller có thể xử lý
            }

            var result = saved > 0
                ? new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, (trackedEvaluation ?? existingEvaluation ?? entity).MapToViewDto())
                : CreateValidationError("CreateEvaluationFailed");
            
            // 🔧 FIX: Thêm debug log để kiểm tra final result
            Console.WriteLine($"DEBUG EVALUATION CREATE: Final result - Status: {result.Status}, Message: {result.Message}");
            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                Console.WriteLine($"DEBUG EVALUATION CREATE: Success! Evaluation created/updated successfully");
            }
            else
            {
                Console.WriteLine($"DEBUG EVALUATION CREATE: Failed! Error: {result.Message}");
            }
            
            Console.WriteLine($"DEBUG EVALUATION CREATE: Returning result - Status: {result.Status}, Message: {result.Message}");
            return result;
        }

        // ================== UPDATE ==================
        public async Task<IServiceResult> UpdateAsync(Guid id, EvaluationUpdateDto dto, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id && !e.IsDeleted
            );
            if (entity == null)
                return CreateValidationError("EvaluationNotFound");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToUpdateEvaluation");

            // 🔧 FIX: Thêm validation cho EvaluationResult
            if (string.IsNullOrWhiteSpace(dto.EvaluationResult))
            {
                Console.WriteLine($"DEBUG UPDATE EVALUATION: EvaluationResult is null or empty: '{dto.EvaluationResult}'");
                return CreateValidationError("EvaluationResultRequired");
            }
            
            // Validate EvaluationResult
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary" };
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"DEBUG UPDATE EVALUATION: Invalid EvaluationResult: '{dto.EvaluationResult}'");
                var parameters = new Dictionary<string, object> { ["result"] = dto.EvaluationResult };
                return CreateValidationError("InvalidEvaluationResultForUpdate", parameters);
            }
            
            // 🔧 MỚI: Validate TotalScore nếu có
            if (dto.TotalScore.HasValue && (dto.TotalScore.Value < 0 || dto.TotalScore.Value > 100))
            {
                Console.WriteLine($"DEBUG UPDATE EVALUATION: Invalid TotalScore: '{dto.TotalScore.Value}'");
                var parameters = new Dictionary<string, object> 
                { 
                    ["TotalScore"] = dto.TotalScore.Value,
                    ["MinValue"] = 0,
                    ["MaxValue"] = 100
                };
                return CreateValidationError("InvalidTotalScoreForUpdate", parameters);
            }

            // Lưu kết quả cũ để so sánh
            var oldResult = entity.EvaluationResult;
            
            // Debug log
            Console.WriteLine($"DEBUG UPDATE EVALUATION: oldResult = '{oldResult}', dto.EvaluationResult = '{dto.EvaluationResult}'");

            // 🔧 CẢI THIỆN: Logic tạo comments thông minh cho update
            string detailedComments;
            if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) && 
                dto.ProblematicSteps?.Any() == true)
            {
                // Nếu là Fail và có problematic steps, tạo format chuẩn
                var problematicStep = dto.ProblematicSteps.First();
                
                // Tạo format đơn giản cho problematic steps
                    detailedComments = dto.Comments ?? "";
                    if (!string.IsNullOrEmpty(dto.DetailedFeedback))
                    {
                        detailedComments += $"\n\nChi tiết vấn đề: {dto.DetailedFeedback}";
                    }
                    if (dto.ProblematicSteps?.Any() == true)
                    {
                        detailedComments += $"\nTiến trình có vấn đề: {string.Join(", ", dto.ProblematicSteps)}";
                    }
                    if (!string.IsNullOrEmpty(dto.Recommendations))
                    {
                        detailedComments += $"\nKhuyến nghị: {dto.Recommendations}";
                }
            }
            else
            {
                // Tạo comments thông thường cho các trường hợp khác
                detailedComments = dto.Comments ?? "";
                if (!string.IsNullOrEmpty(dto.DetailedFeedback))
                {
                    detailedComments += $"\n\nChi tiết vấn đề: {dto.DetailedFeedback}";
                }
                if (dto.ProblematicSteps?.Any() == true)
                {
                    detailedComments += $"\nTiến trình có vấn đề: {string.Join(", ", dto.ProblematicSteps)}";
                }
                if (!string.IsNullOrEmpty(dto.Recommendations))
                {
                    detailedComments += $"\nKhuyến nghị: {dto.Recommendations}";
                }
            }

            // 🔧 CẢI THIỆN: Cập nhật EvaluatedBy nếu là expert và chưa có
            if (isExpert && entity.EvaluatedBy == null)
            {
                var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                    predicate: e => e.UserId == userId && !e.IsDeleted,
                    asNoTracking: true
                );
                if (expert != null)
                {
                    entity.EvaluatedBy = expert.ExpertId;
                    Console.WriteLine($"DEBUG EVALUATION UPDATE: Set EvaluatedBy to expertId: {expert.ExpertId}");
                }
            }
            
            entity.EvaluationResult = dto.EvaluationResult;
            // 🔧 MỚI: Cập nhật điểm số nếu có
            if (dto.TotalScore.HasValue)
            {
                entity.TotalScore = dto.TotalScore.Value;
            }
            entity.Comments = detailedComments.Trim();
            if (dto.EvaluatedAt.HasValue) entity.EvaluatedAt = dto.EvaluatedAt.Value;
            entity.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(entity);

            // Xử lý logic workflow khi evaluation result thay đổi
            if (dto.EvaluationResult != null)
            {
                Console.WriteLine($"DEBUG EVALUATION UPDATE: Result changed from '{oldResult}' to '{dto.EvaluationResult}'");
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(entity.BatchId);
                Console.WriteLine($"DEBUG EVALUATION UPDATE: Current batch status: {batch?.Status}");
                
                if (batch != null)
                {
                    if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"DEBUG EVALUATION UPDATE: Processing Fail evaluation");
                        // Nếu đánh giá Fail, chuyển batch về trạng thái InProgress
                        if (batch.Status == "Completed" || batch.Status == "AwaitingEvaluation")
                        {
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: Changing status from {batch.Status} to InProgress");
                            batch.Status = "InProgress";
                            batch.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: Status changed successfully to InProgress");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: No status change needed for Fail. Current status: {batch.Status}");
                        }
                    }
                    else if (dto.EvaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"DEBUG EVALUATION UPDATE: Processing Pass evaluation");
                        bool statusChanged = false;
                        
                        // Nếu đánh giá Pass và batch đang AwaitingEvaluation, chuyển sang Completed
                        if (batch.Status == "AwaitingEvaluation")
                        {
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: Changing status from AwaitingEvaluation to Completed");
                            batch.Status = "Completed";
                            batch.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                            statusChanged = true;
                        }
                        // Nếu đánh giá Pass và batch đang InProgress, chuyển sang Completed
                        else if (batch.Status == "InProgress")
                        {
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: Changing status from InProgress to Completed");
                            batch.Status = "Completed";
                            batch.UpdatedAt = DateTime.UtcNow;
                            await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                            statusChanged = true;
                        }

                        // Nếu status đã chuyển sang Completed, chỉ cập nhật trạng thái
                        if (statusChanged)
                        {
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: Status changed successfully to Completed");
                        }
                        else
                        {
                            Console.WriteLine($"DEBUG EVALUATION UPDATE: No status change needed. Current status: {batch.Status}");
                        }
                    }
                }
            }

            Console.WriteLine($"DEBUG EVALUATION UPDATE: About to save changes...");
            var saved = await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"DEBUG EVALUATION UPDATE: Save result: {saved}");
            
            // 🔧 FIX: Kiểm tra xem evaluation có thực sự được lưu không
            if (saved > 0)
            {
                var savedEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                    e => e.EvaluationId == id && !e.IsDeleted
                );
                if (savedEvaluation != null)
                {
                    Console.WriteLine($"DEBUG EVALUATION UPDATE: Verification - Saved EvaluationResult: '{savedEvaluation.EvaluationResult}', Comments: '{savedEvaluation.Comments}'");
                }
                else
                {
                    Console.WriteLine($"DEBUG EVALUATION UPDATE: ERROR - Could not find saved evaluation after save!");
                }
            }

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công.", entity.MapToViewDto())
                : CreateValidationError("UpdateEvaluationFailed");
        }

        // ================== DELETE (soft) ==================
        public async Task<IServiceResult> DeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id && !e.IsDeleted
            );
            if (entity == null)
                return CreateValidationError("EvaluationNotFoundForDelete");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToDeleteEvaluation");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công.")
                : CreateValidationError("SoftDeleteEvaluationFailed");
        }

        // ================== HARD DELETE ==================
        public async Task<IServiceResult> HardDeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id
            );
            if (entity == null)
                return CreateValidationError("EvaluationNotFoundForHardDelete");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToHardDeleteEvaluation");

            // Chỉ Admin mới được xóa cứng
            if (!isAdmin)
                return CreateValidationError("OnlyAdminCanHardDeleteEvaluation");

            await _unitOfWork.ProcessingBatchEvaluationRepository.RemoveAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá cứng thành công.")
                : CreateValidationError("HardDeleteEvaluationFailed");
        }

        // ================== BULK HARD DELETE ==================
        public async Task<IServiceResult> BulkHardDeleteAsync(List<Guid> ids, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            if (ids == null || !ids.Any())
                return CreateValidationError("InvalidBulkDeleteIds");

            // Chỉ Admin mới được xóa cứng hàng loạt
            if (!isAdmin)
                return CreateValidationError("OnlyAdminCanBulkHardDeleteEvaluation");

            var entities = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => ids.Contains(e.EvaluationId)
            );

            if (!entities.Any())
                return CreateValidationError("NoEvaluationsFoundForBulkDelete");

            // Kiểm tra quyền cho từng entity
            foreach (var entity in entities)
            {
                var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
                if (!canAccess)
                {
                    var parameters = new Dictionary<string, object> { ["evaluationId"] = entity.EvaluationId };
                    return CreateValidationError("NoPermissionToDeleteSpecificEvaluation", parameters);
                }
            }

            foreach (var entity in entities)
            {
                await _unitOfWork.ProcessingBatchEvaluationRepository.RemoveAsync(entity);
            }

            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, $"Đã xoá cứng {saved} đánh giá thành công.")
                : CreateValidationError("BulkHardDeleteEvaluationFailed");
        }

        // ================== RESTORE ==================
        public async Task<IServiceResult> RestoreAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id && e.IsDeleted
            );
            if (entity == null)
                return CreateValidationError("EvaluationNotFoundForRestore");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToRestoreEvaluation");

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Khôi phục đánh giá thành công.", entity.MapToViewDto())
                : CreateValidationError("RestoreEvaluationFailed");
        }

        // ================== GET ALL ==================
        public async Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            // Chỉ Admin, Manager, Expert mới có quyền xem tất cả evaluations
            if (!isAdmin && !isManager && !isExpert)
                return CreateValidationError("NoPermissionToViewAllEvaluations");

            var list = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => !e.IsDeleted,
                include: q => q.Include(e => e.Batch)
                    .ThenInclude(b => b.Farmer)
                    .ThenInclude(f => f.User)
                    .Include(e => e.Batch)
                    .ThenInclude(b => b.Method),
                orderBy: q => q.OrderByDescending(x => x.EvaluatedAt).ThenByDescending(x => x.CreatedAt),
                asNoTracking: true
            );

            var dtos = list.Select(x => x.MapToViewDto()).ToList();
            
            // Lấy thông tin expert name cho từng evaluation
            foreach (var dto in dtos)
            {
                if (dto.EvaluatedBy.HasValue)
                {
                    // Sử dụng EvaluatedBy field để lấy expert ID
                    var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                        predicate: e => e.ExpertId == dto.EvaluatedBy.Value && !e.IsDeleted,
                        include: q => q.Include(e => e.User),
                        asNoTracking: true
                    );
                    dto.ExpertName = expert?.User?.Name;
                }
            }
            
            return dtos.Any()
                ? new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos)
                : new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<EvaluationViewDto>());
        }

        // ================== LIST BY BATCH ==================
        public async Task<IServiceResult> GetByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var canAccess = await HasPermissionToAccessAsync(batchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToViewBatchEvaluations");

            var list = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => !e.IsDeleted && e.BatchId == batchId,
                include: q => q.Include(e => e.Batch)
                    .ThenInclude(b => b.Farmer)
                    .ThenInclude(f => f.User),
                orderBy: q => q.OrderByDescending(x => x.EvaluatedAt).ThenByDescending(x => x.CreatedAt),
                asNoTracking: true
            );

            var dtos = list.Select(x => x.MapToViewDto()).ToList();
            
            // Lấy thông tin expert name cho từng evaluation
            foreach (var dto in dtos)
            {
                if (dto.EvaluatedBy.HasValue)
                {
                    // Sử dụng EvaluatedBy field để lấy expert ID
                    var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                        predicate: e => e.ExpertId == dto.EvaluatedBy.Value && !e.IsDeleted,
                        include: q => q.Include(e => e.User),
                        asNoTracking: true
                    );
                    dto.ExpertName = expert?.User?.Name;
                }
            }
            
            return dtos.Any()
                ? new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos)
                : new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG, new List<EvaluationViewDto>());
        }

        // ================== SUMMARY ==================
        public async Task<IServiceResult> GetSummaryByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var canAccess = await HasPermissionToAccessAsync(batchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToViewEvaluationSummary");

            var list = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => !e.IsDeleted && e.BatchId == batchId,
                asNoTracking: true
            );

            var total = list.Count;
            var passCount = list.Count(x => x.EvaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase));
            var failCount = list.Count(x => x.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase));
            var needsImprovementCount = list.Count(x => x.EvaluationResult.Equals("NeedsImprovement", StringComparison.OrdinalIgnoreCase));
            var temporaryCount = list.Count(x => x.EvaluationResult.Equals("Temporary", StringComparison.OrdinalIgnoreCase));
            var latest = list.OrderByDescending(x => x.EvaluatedAt ?? x.CreatedAt).FirstOrDefault();

            var summary = new EvaluationSummaryDto
            {
                BatchId = batchId,
                Total = total,
                PassCount = passCount,
                FailCount = failCount,
                LatestResult = latest?.EvaluationResult,
                LatestAt = latest?.EvaluatedAt
            };

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, summary);
        }

        // ================== LIST DELETED BY BATCH ==================
        public async Task<IServiceResult> GetDeletedByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var canAccess = await HasPermissionToAccessAsync(batchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return CreateValidationError("NoPermissionToViewDeletedEvaluations");

            var list = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => e.IsDeleted && e.BatchId == batchId,
                orderBy: q => q.OrderByDescending(x => x.UpdatedAt),
                asNoTracking: true
            );

            var dtos = list.Select(x => x.MapToViewDto()).ToList();
            return dtos.Any()
                ? new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách đánh giá đã xóa thành công.", dtos)
                : new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có đánh giá nào đã xóa.", new List<EvaluationViewDto>());
        }

                 // ================== GET FAILED STAGES FOR BATCH ==================
         public async Task<List<ProcessingStage>> GetFailedStagesForBatchAsync(Guid batchId)
        {
            try
            {
                Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Starting for batchId: {batchId}");
                
                // Lấy evaluation cuối cùng của batch
                var latestEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId == batchId && !e.IsDeleted,
                    orderBy: q => q.OrderByDescending(e => e.CreatedAt)
                );

                var evaluation = latestEvaluation.FirstOrDefault();
                                 if (evaluation == null || evaluation.EvaluationResult != "Fail")
                 {
                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: No fail evaluation found for batch {batchId}");
                     return new List<ProcessingStage>();
                 }

                Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found evaluation with comments: {evaluation.Comments}");

                                 // Parse failed stages từ comments
                 var failedStages = new List<ProcessingStage>();
                 var comments = evaluation.Comments ?? "";
                
                Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Full comments: {comments}");
                
                                 // 🔧 FIX: Tìm cả 3 pattern - cũ, mới và mới nhất
                // Pattern 1: "Giai đoạn cần cập nhật: [danh sách]" (cũ)
                var match1 = System.Text.RegularExpressions.Regex.Match(comments, @"Giai đoạn cần cập nhật:\s*([^\n]+)");
                
                // Pattern 2: "🔧 Giai đoạn cần cập nhật: [danh sách]" (mới)
                var match2 = System.Text.RegularExpressions.Regex.Match(comments, @"🔧 Giai đoạn cần cập nhật:\s*([^\n]+)");
                
                 // Pattern 3: "🔧 Giai đoạn cần cập nhật: StageId: 1, StageId: 2, StageId: 3" (mới nhất)
                 var match3 = System.Text.RegularExpressions.Regex.Match(comments, @"🔧 Giai đoạn cần cập nhật:\s*([^\n]+)");
                 
                 if (match3.Success)
                 {
                     // Ưu tiên pattern mới nhất (🔧 với StageId format)
                     var stagesText = match3.Groups[1].Value.Trim();
                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found NEWEST pattern stages text: {stagesText}");
                     
                     // Parse stages từ format "StageId: 1, StageId: 2, StageId: 3, StageId: 4"
                     var stageIdMatches = System.Text.RegularExpressions.Regex.Matches(stagesText, @"StageId:\s*(\d+)");
                     foreach (System.Text.RegularExpressions.Match stageIdMatch in stageIdMatches)
                     {
                         var stageIdStr = stageIdMatch.Groups[1].Value.Trim();
                         
                         if (int.TryParse(stageIdStr, out var stageId))
                         {
                             // Tìm stage từ database theo StageId
                             var stageFromDb = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(stageId);
                             if (stageFromDb != null && !stageFromDb.IsDeleted)
                             {
                                 if (!failedStages.Any(s => s.StageId == stageId))
                                 {
                                     failedStages.Add(stageFromDb);
                                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Added stage from NEWEST pattern: {stageFromDb.StageName} (ID: {stageId})");
                                 }
                             }
                             else
                             {
                                 Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Stage not found in database for ID: {stageId}");
                             }
                         }
                     }
                 }
                 else if (match2.Success)
                 {
                     // Fallback về pattern mới (🔧 với tên stage)
                     var stagesText = match2.Groups[1].Value.Trim();
                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found NEW pattern stages text: {stagesText}");
                     
                                           // Parse stages từ format "Thu hoạch (StageId: xxx, Thứ tự: 1), Phơi (StageId: xxx, Thứ tự: 2)"
                      var stageMatches = System.Text.RegularExpressions.Regex.Matches(stagesText, @"([^(]+)\s*\(StageId:\s*([^,]+),\s*Thứ tự:\s*(\d+)\)");
                     foreach (System.Text.RegularExpressions.Match stageMatch in stageMatches)
                     {
                         var stageName = stageMatch.Groups[1].Value.Trim();
                         var stageIdStr = stageMatch.Groups[2].Value.Trim();
                         var orderIndexStr = stageMatch.Groups[3].Value.Trim();
                         
                                                                    if (!string.IsNullOrEmpty(stageName) && int.TryParse(stageIdStr, out var stageId))
                      {
                          // Tìm stage từ database theo StageId
                          var stageFromDb = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(stageId);
                          if (stageFromDb != null && !stageFromDb.IsDeleted)
                          {
                              if (!failedStages.Any(s => s.StageId == stageId))
                              {
                                  failedStages.Add(stageFromDb);
                                  Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Added stage from database: {stageFromDb.StageName} (ID: {stageId})");
                              }
                          }
                          else
                          {
                              // Fallback: tạo stage mới nếu không tìm thấy trong database
                              var stage = new ProcessingStage
                              {
                                  StageId = stageId,
                                  StageName = stageName,
                                  OrderIndex = int.Parse(orderIndexStr)
                              };
                              
                              if (!failedStages.Any(s => s.StageId == stageId))
                              {
                                  failedStages.Add(stage);
                                  Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Added stage from NEW pattern (fallback): {stageName} (ID: {stageId})");
                              }
                          }
                      }
                     }
                 }
                                 else if (match1.Success)
                 {
                     // Fallback về pattern cũ - lấy stages từ database dựa trên tên
                     var stagesText = match1.Groups[1].Value.Trim();
                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found OLD pattern stages text: {stagesText}");
                     
                     var stageNames = stagesText.Split(',').Select(s => s.Trim()).ToList();
                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Parsed stage names from OLD pattern: {string.Join(", ", stageNames)}");
                     
                     // 🔧 FIX: Nếu có "PB", thay thế bằng stages cụ thể
                     if (stageNames.Contains("PB"))
                     {
                         Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found 'PB', replacing with specific stages");
                         
                         // Lấy batch để có MethodId
                         var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                         if (batch != null)
                         {
                             // Lấy tất cả stages theo MethodId
                             var allStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                                 s => s.MethodId == batch.MethodId && !s.IsDeleted,
                                 q => q.OrderBy(s => s.OrderIndex));
                             
                             Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found {allStages.Count()} stages for MethodId {batch.MethodId}");
                             
                             // Thay thế "PB" bằng stages cụ thể từ database
                             stageNames.Remove("PB");
                             failedStages.AddRange(allStages);
                             
                             Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: After replacing PB: {string.Join(", ", allStages.Select(s => s.StageName))}");
                         }
                         else
                         {
                             // Fallback: sử dụng stages mặc định từ database
                             stageNames.Remove("PB");
                             var defaultStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                                 s => !s.IsDeleted,
                                 q => q.OrderBy(s => s.OrderIndex));
                             failedStages.AddRange(defaultStages.Take(5));
                             Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Using default stages: {string.Join(", ", defaultStages.Take(5).Select(s => s.StageName))}");
                         }
                     }
                     else
                     {
                         // Tìm stages theo tên từ database
                         var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                         if (batch != null)
                         {
                             var allStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                                 s => s.MethodId == batch.MethodId && !s.IsDeleted,
                                 q => q.OrderBy(s => s.OrderIndex));
                             
                             foreach (var stageName in stageNames)
                             {
                                 var stage = allStages.FirstOrDefault(s => s.StageName.Equals(stageName, StringComparison.OrdinalIgnoreCase));
                                 if (stage != null && !failedStages.Any(s => s.StageId == stage.StageId))
                                 {
                                     failedStages.Add(stage);
                                     Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Found stage by name: {stage.StageName} (ID: {stage.StageId})");
                                 }
                             }
                         }
                     }
                 }
                else
                {
                    Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: No 'Giai đoạn cần cập nhật' pattern found in comments");
                }

                                 Console.WriteLine($"DEBUG GetFailedStagesForBatchAsync: Final result: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");
                 return failedStages;
            }
                         catch (Exception ex)
             {
                 Console.WriteLine($"❌ Lỗi lấy thông tin failed stages: {ex.Message}");
                 return new List<ProcessingStage>();
             }
        }

        // ========== HELPER METHODS CHO ĐÁNH GIÁ CHẤT LƯỢNG ==========

        /// <summary>
        /// Tạo comment đánh giá chất lượng theo format mới
        /// </summary>
        /// <param name="criteria">Danh sách tiêu chí đánh giá với actual values</param>
        /// <param name="expertNotes">Ghi chú của expert</param>
        /// <returns>Comment đã format theo format mới</returns>
        private string CreateQualityEvaluationComment(List<QualityCriteriaEvaluationDto> criteria, string? expertNotes)
        {
            // Tự động đánh giá các tiêu chí dựa trên actual values
            var criteriaResults = QualityEvaluationHelper.AutoEvaluateCriteria(criteria);

            // Sử dụng QualityEvaluationHelper để tạo comment theo format mới
            return QualityEvaluationHelper.CreateQualityEvaluationComment(criteriaResults, expertNotes);
        }

                 // ========== HELPER METHODS CHO ĐÁNH GIÁ CHẤT LƯỢNG ==========
         
                   /// <summary>
          /// Lấy danh sách stage bị fail từ tiêu chí đánh giá
          /// </summary>
          /// <param name="criteria">Danh sách tiêu chí đánh giá</param>
          /// <param name="batchId">ID của batch để lấy MethodId</param>
          /// <returns>Danh sách stage cần retry</returns>
          private async Task<List<ProcessingStage>> GetFailedStagesFromCriteriaAsync(List<QualityCriteriaEvaluationDto> criteria, Guid batchId)
                   {
              var failedStages = new List<ProcessingStage>();
              
              Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Starting with {criteria.Count} criteria");
              
              // 🔧 CẢI THIỆN: Phân tích chi tiết các tiêu chí fail để xác định stages cụ thể
              var failedCriteria = criteria.Where(c => !c.IsPassed).ToList();
              
              Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Found {failedCriteria.Count} failed criteria");
              
              if (failedCriteria.Any())
              {
                  // Phân tích từng tiêu chí fail để xác định stage có vấn đề
                  foreach (var criterion in failedCriteria)
                  {
                      Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Processing failed criterion: {criterion.CriteriaName}");
                       var stageId = await GetStageIdFromFailedCriteriaAsync(criterion, batchId);
                       Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Stage ID for {criterion.CriteriaName}: {stageId}");
                       
                                              if (stageId.HasValue)
                        {
                            // Tìm stage từ database theo StageId
                            var stage = await _unitOfWork.ProcessingStageRepository.GetByIdAsync(stageId.Value);
                            if (stage != null && !stage.IsDeleted && !failedStages.Any(s => s.StageId == stage.StageId))
                            {
                                failedStages.Add(stage);
                                Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Added stage: {stage.StageName} (ID: {stage.StageId})");
                           }
                       }
                  }
              }
             
                           // 🔧 CẢI THIỆN: Nếu không xác định được stage cụ thể, lấy tất cả stages từ MethodId
              if (!failedStages.Any())
              {
                  Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: No specific stages found, getting all stages from MethodId");
                  
                  // Lấy batch để có MethodId
                  var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                  if (batch != null)
                  {
                      Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Found batch with MethodId: {batch.MethodId}");
                      
                      // Lấy tất cả stages theo MethodId
                      var allStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                          s => s.MethodId == batch.MethodId && !s.IsDeleted,
                          q => q.OrderBy(s => s.OrderIndex));
                      
                      Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Found {allStages.Count()} stages in database");
                      
                      foreach (var stage in allStages)
                      {
                          Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Stage from DB: {stage.StageId} - {stage.StageName}");
                      }
                      
                      failedStages.AddRange(allStages);
                     
                                           // 🔧 FIX: Nếu không lấy được stages từ database, thử lấy tất cả stages
                      if (!allStages.Any())
                      {
                          Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: No stages found for MethodId {batch.MethodId}, trying to get all stages");
                          
                          var allStagesInSystem = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                              s => !s.IsDeleted,
                              q => q.OrderBy(s => s.OrderIndex));
                          
                          Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Found {allStagesInSystem.Count()} total stages in system");
                          
                          foreach (var stage in allStagesInSystem)
                          {
                              Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: All stage from DB: {stage.StageId} - {stage.StageName} (MethodId: {stage.MethodId})");
                          }
                          
                          // Lấy 3 stages đầu tiên làm default
                          var defaultStages = allStagesInSystem.Take(3).ToList();
                          failedStages.AddRange(defaultStages);
                          Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Using default stages: {string.Join(", ", defaultStages.Select(s => s.StageName))}");
                      }
                 }
                 else
                 {
                     Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Batch not found for batchId: {batchId}");
                 }
             }
             
                           // 🔧 FIX: Nếu vẫn không có stages, sử dụng default stages dựa trên tiêu chí fail
              if (!failedStages.Any())
              {
                  Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Still no stages, using default stages based on failed criteria");
                  
                                     // 🔧 LOẠI BỎ HOÀN TOÀN HARD CODE: Chỉ sử dụng StageId từ database
                  // Lấy batch để có MethodId
                  var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                  if (batch != null)
                  {
                      // Lấy tất cả stages theo MethodId
                      var allStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                          s => s.MethodId == batch.MethodId && !s.IsDeleted,
                          q => q.OrderBy(s => s.OrderIndex));
                      
                      Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Found {allStages.Count()} stages for MethodId {batch.MethodId}");
                      
                       // 🔧 LOẠI BỎ HOÀN TOÀN HARD CODE: Chỉ sử dụng StageId từ database
                       // Không phân tích tên tiêu chí nữa, chỉ dựa vào mapping từ SystemConfiguration
                       Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: No specific stage mapping found, using all stages");
                       
                       // Nếu không có mapping cụ thể, lấy tất cả stages
                       failedStages.AddRange(allStages);
                       Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Using all stages: {string.Join(", ", allStages.Select(s => s.StageName))}");
                  }
              }
             
                           Console.WriteLine($"DEBUG GetFailedStagesFromCriteriaAsync: Final failed stages: {string.Join(", ", failedStages.Select(s => $"{s.StageName} (ID: {s.StageId})"))}");
              return failedStages;
         }
         
                   /// <summary>
          /// Xác định stage code từ tiêu chí fail
          /// </summary>
          /// <param name="criterion">Tiêu chí bị fail</param>
          /// <returns>Stage code có vấn đề</returns>
                     /// <summary>
           /// Xác định stage ID từ tiêu chí fail - Sử dụng mapping động từ SystemConfiguration
           /// </summary>
           /// <param name="criterion">Tiêu chí bị fail</param>
           /// <param name="batchId">ID của batch để lấy MethodId</param>
           /// <returns>Stage ID có vấn đề</returns>
           private async Task<int?> GetStageIdFromFailedCriteriaAsync(QualityCriteriaEvaluationDto criterion, Guid batchId)
          {
              if (string.IsNullOrEmpty(criterion.CriteriaName))
                    return null;
                
                try
                {
                                         // 🔧 CẢI THIỆN: Lấy mapping từ SystemConfiguration thay vì hard code
                     var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                     if (batch == null) return null;
                     
                     // Lấy tất cả criteria configurations từ SystemConfiguration
                     var criteriaConfigs = await _unitOfWork.SystemConfigurationRepository.GetAllAsync(
                         c => !c.IsDeleted && c.TargetEntity == "ProcessingBatch"
                     );
                     
                     // Tìm criteria config tương ứng với criterion bị fail
                     var matchingConfig = criteriaConfigs.FirstOrDefault(c => 
                         c.Name?.Equals(criterion.CriteriaName, StringComparison.OrdinalIgnoreCase) == true);
                     
                     if (matchingConfig != null)
                     {
                         // 🔧 MỚI: Lấy StageId từ TargetField hoặc ScopeId nếu có
                         if (!string.IsNullOrEmpty(matchingConfig.TargetField) && int.TryParse(matchingConfig.TargetField, out var stageId))
                         {
                             Console.WriteLine($"DEBUG GetStageIdFromFailedCriteriaAsync: Found mapping for {criterion.CriteriaName} -> StageId: {stageId}");
                             return stageId;
                         }
                         
                                                   // Fallback: Lấy từ ScopeId nếu có (ScopeId là Guid, không thể convert sang int)
                          if (matchingConfig.ScopeId.HasValue)
                          {
                              Console.WriteLine($"DEBUG GetStageIdFromFailedCriteriaAsync: Found mapping for {criterion.CriteriaName} -> ScopeId: {matchingConfig.ScopeId}");
                              // ScopeId là Guid, không thể convert sang int, bỏ qua
                              return null;
                          }
                     }
                    
                    // 🔧 FALLBACK: Nếu không tìm thấy mapping, sử dụng logic phân tích từ tên tiêu chí
                    Console.WriteLine($"DEBUG GetStageIdFromFailedCriteriaAsync: No mapping found for {criterion.CriteriaName}, using fallback logic");
                    
                    // Phân tích tên tiêu chí để xác định stage
                    var criteriaName = criterion.CriteriaName.ToUpper();
                    
                    // Tìm stage có tên tương ứng với tiêu chí
                    var allStages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                        s => s.MethodId == batch.MethodId && !s.IsDeleted,
                        q => q.OrderBy(s => s.OrderIndex)
                    );
                    
                    // Logic phân tích thông minh dựa trên tên tiêu chí và thứ tự stage
                                         // 🔧 LOẠI BỎ HOÀN TOÀN HARD CODE: Chỉ sử dụng StageId từ database
                     // Không phân tích tên tiêu chí nữa, chỉ dựa vào mapping từ SystemConfiguration
                     Console.WriteLine($"DEBUG GetStageIdFromFailedCriteriaAsync: No mapping found for {criterion.CriteriaName}, returning null");
                     return null;
                    
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG GetStageIdFromFailedCriteriaAsync: Error getting stage mapping: {ex.Message}");
                    return null;
              }
          }
    }
}



