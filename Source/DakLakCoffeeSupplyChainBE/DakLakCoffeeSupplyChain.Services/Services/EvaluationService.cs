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

            // Validate EvaluationResult
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary", "Pending" };
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
                return CreateValidationError("InvalidEvaluationResult");

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

            var entity = new ProcessingBatchEvaluation
            {
                EvaluationId = Guid.NewGuid(),
                EvaluationCode = code,
                BatchId = dto.BatchId,
                EvaluatedBy = expertId, // Lưu ExpertId thay vì UserId
                EvaluationResult = dto.EvaluationResult,
                Comments = detailedComments.Trim(),
                EvaluatedAt = dto.EvaluatedAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Nếu là farmer tạo đơn đánh giá, set EvaluationResult = "Pending"
            if (!isAdmin && !isManager && !isExpert)
            {
                entity.EvaluationResult = "Pending"; // Đơn đánh giá chờ expert xử lý
                entity.EvaluatedAt = null; // Chưa được đánh giá
            }

            // Xóa evaluation tự động cũ (nếu có) trước khi tạo evaluation mới
            var existingAutoEvaluations = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => e.BatchId == dto.BatchId && e.EvaluatedBy == null && !e.IsDeleted
            );
            
            foreach (var autoEval in existingAutoEvaluations)
            {
                autoEval.IsDeleted = true;
                autoEval.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(autoEval);
            }

            int saved = 0;
            try
            {
                await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(entity);

                // Xử lý logic workflow theo kết quả đánh giá
                if (dto.EvaluationResult.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    // Nếu farmer tạo đơn đánh giá, chuyển batch sang AwaitingEvaluation
                    if (batch.Status == "Completed")
                    {
                        batch.Status = "AwaitingEvaluation";
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    }
                }
                else if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
                {
                    // 🔧 MỚI: Xử lý logic retry khi evaluation fail
                    if (batch.Status == "Completed" || batch.Status == "AwaitingEvaluation")
                    {
                        // Chuyển batch về trạng thái InProgress để farmer cập nhật
                        batch.Status = "InProgress";
                        batch.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                        
                        // 🔧 MỚI: Lưu thông tin về các stage cần cập nhật
                        if (dto.ProblematicSteps?.Any() == true)
                        {
                            await SaveFailedStagesInfoAsync(dto.BatchId, dto.ProblematicSteps);
                        }
                        
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
                else if (dto.EvaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase))
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

                    // 🔧 MỚI: Xóa thông tin retry khi batch được đánh giá thành công
                    if (statusChanged)
                    {
                        await ClearRetryInfoAsync(dto.BatchId);
                    }
                }

                saved = await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"Lỗi tạo evaluation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw để controller có thể xử lý
            }

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, entity.MapToViewDto())
                : CreateValidationError("CreateEvaluationFailed");
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

        // ========== HELPER METHODS CHO RETRY LOGIC ==========

        /// <summary>
        /// Lưu thông tin về các stage cần cập nhật khi evaluation fail
        /// </summary>
        /// <param name="batchId">ID của batch</param>
        /// <param name="problematicSteps">Danh sách stage cần cập nhật</param>
        private async Task SaveFailedStagesInfoAsync(Guid batchId, List<string> problematicSteps)
        {
            try
            {
                // Tạo một record trong SystemConfiguration để lưu thông tin retry
                var retryInfo = new SystemConfiguration
                {
                    Name = $"RETRY_INFO_{batchId}",
                    Description = string.Join("|", problematicSteps), // Lưu danh sách stages trong Description
                    TargetEntity = "ProcessingBatch",
                    TargetField = "FailedStages",
                    IsActive = true,
                    EffectedDateFrom = DateTime.UtcNow,
                    EffectedDateTo = DateTime.UtcNow.AddDays(30), // Tự động xóa sau 30 ngày
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.SystemConfigurationRepository.CreateAsync(retryInfo);
                await _unitOfWork.SaveChangesAsync();
                
                Console.WriteLine($"✅ Đã lưu thông tin retry cho batch {batchId}: {string.Join(", ", problematicSteps)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi lưu thông tin retry: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy thông tin về các stage cần cập nhật cho batch
        /// </summary>
        /// <param name="batchId">ID của batch</param>
        /// <returns>Danh sách stage cần cập nhật</returns>
        public async Task<List<string>> GetFailedStagesForBatchAsync(Guid batchId)
        {
            try
            {
                var retryInfo = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => c.Name == $"RETRY_INFO_{batchId}" && c.IsActive && !c.IsDeleted,
                    asNoTracking: true
                );

                if (retryInfo != null && !string.IsNullOrEmpty(retryInfo.Description))
                {
                    return retryInfo.Description.Split('|').ToList();
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi lấy thông tin retry: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Xóa thông tin retry khi batch được đánh giá thành công
        /// </summary>
        /// <param name="batchId">ID của batch</param>
        private async Task ClearRetryInfoAsync(Guid batchId)
        {
            try
            {
                var retryInfo = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(
                    predicate: c => c.Name == $"RETRY_INFO_{batchId}" && !c.IsDeleted,
                    asNoTracking: false
                );

                if (retryInfo != null)
                {
                    retryInfo.IsDeleted = true;
                    retryInfo.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SystemConfigurationRepository.UpdateAsync(retryInfo);
                    await _unitOfWork.SaveChangesAsync();
                    
                    Console.WriteLine($"✅ Đã xóa thông tin retry cho batch {batchId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi xóa thông tin retry: {ex.Message}");
            }
        }
    }
}



