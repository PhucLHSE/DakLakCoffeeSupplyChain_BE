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
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền tạo đánh giá cho lô này.");

            // Tồn tại batch?
            var batchExists = await _unitOfWork.ProcessingBatchRepository.AnyAsync(
                b => b.BatchId == dto.BatchId && !b.IsDeleted
            );
            if (!batchExists)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Lô sơ chế không hợp lệ.");

            // Validate EvaluationResult
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary", "Pending" };
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Kết quả đánh giá không hợp lệ. Chỉ chấp nhận: Pass, Fail, NeedsImprovement, Temporary, Pending.");

            // Kiểm tra batch status trước khi đánh giá
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId);
            if (batch == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy lô sơ chế.");

            // Cho phép Expert tạo đánh giá cho batch đã hoàn thành, đang chờ đánh giá, hoặc đang xử lý
            // Cho phép Admin/Manager tạo đánh giá cho mọi trạng thái
            // Cho phép Farmer tạo đơn đánh giá khi batch đã hoàn thành
            if (isExpert)
            {
                // Expert có thể tạo đánh giá cho batch Completed, AwaitingEvaluation, hoặc InProgress
                if (batch.Status != "Completed" && batch.Status != "AwaitingEvaluation" && batch.Status != "InProgress")
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ có thể tạo đánh giá cho lô đã hoàn thành, đang chờ đánh giá, hoặc đang xử lý.");
            }
            else if (isAdmin || isManager)
            {
                // Admin/Manager có thể tạo đánh giá cho mọi trạng thái
            }
            else
            {
                // Farmer chỉ có thể tạo đơn đánh giá khi batch đã hoàn thành
                if (batch.Status != "Completed")
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ có thể tạo đơn đánh giá cho lô đã hoàn thành.");
            }

            var code = await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year);

            // Tạo comments chi tiết bao gồm thông tin đơn yêu cầu đánh giá và tiến trình
            var detailedComments = dto.Comments ?? "";
            
            // Nếu là Fail và có thông tin stage cụ thể, tạo format chuẩn
            if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) && 
                dto.ProblematicSteps?.Any() == true)
            {
                // Lấy thông tin stage đầu tiên có vấn đề
                var problematicStep = dto.ProblematicSteps.First();
                
                // Parse để lấy StageId từ format "Step X: StageName" hoặc "StageName"
                var stageName = problematicStep.Contains(":") 
                    ? problematicStep.Split(':').Last().Trim() 
                    : problematicStep.Trim();
                
                // Tìm StageId từ tên stage
                var stage = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.StageName.Contains(stageName) && s.MethodId == batch.MethodId && !s.IsDeleted
                );
                
                if (stage.Any())
                {
                    var failedStage = stage.First();
                    var failureDetails = dto.DetailedFeedback ?? "Không đạt tiêu chuẩn";
                    var recommendations = dto.Recommendations ?? "Cần cải thiện";
                    
                    // Tạo format comments chuẩn cho failure
                    detailedComments = StageFailureParser.CreateFailureComment(
                        failedStage.StageId,
                        failedStage.StageName,
                        failureDetails,
                        recommendations
                    );
                }
                else
                {
                    // Fallback: tạo comments thông thường
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
            }
            else
            {
                // Thêm thông tin đơn yêu cầu đánh giá nếu có
                if (!string.IsNullOrEmpty(dto.RequestReason))
                {
                    detailedComments += $"\n\nLý do yêu cầu đánh giá: {dto.RequestReason}";
                }
                if (!string.IsNullOrEmpty(dto.AdditionalNotes))
                {
                    detailedComments += $"\nGhi chú bổ sung: {dto.AdditionalNotes}";
                }
                
                // Thêm thông tin đánh giá chi tiết nếu có
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

            // Lấy ExpertId từ UserId nếu là expert
            Guid? expertId = null;
            if (isExpert)
            {
                var expert = await _unitOfWork.AgriculturalExpertRepository.GetByIdAsync(
                    predicate: e => e.UserId == userId && !e.IsDeleted,
                    asNoTracking: true
                );
                expertId = expert?.ExpertId;
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
                // Nếu đánh giá Fail, chuyển batch về trạng thái InProgress để farmer sửa
                if (batch.Status == "Completed" || batch.Status == "AwaitingEvaluation")
                {
                    batch.Status = "InProgress";
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
                    
                    // Gửi notification cho Farmer
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

                                 // Nếu status đã chuyển sang Completed, chỉ cập nhật trạng thái
                 if (statusChanged)
                 {
                     // Batch đã được chuyển sang Completed thành công
                 }
             }

            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, entity.MapToViewDto())
                : new ServiceResult(Const.FAIL_CREATE_CODE, "Tạo đánh giá thất bại.");
        }

        // ================== UPDATE ==================
        public async Task<IServiceResult> UpdateAsync(Guid id, EvaluationUpdateDto dto, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id && !e.IsDeleted
            );
            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy đánh giá.");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền cập nhật đánh giá này.");

            // Validate EvaluationResult
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary" };
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Kết quả đánh giá không hợp lệ. Chỉ chấp nhận: Pass, Fail, NeedsImprovement, Temporary.");

            // Lưu kết quả cũ để so sánh
            var oldResult = entity.EvaluationResult;
            
            // Debug log
            Console.WriteLine($"DEBUG UPDATE EVALUATION: oldResult = '{oldResult}', dto.EvaluationResult = '{dto.EvaluationResult}'");

            // Tạo comments chi tiết bao gồm thông tin tiến trình
            var detailedComments = dto.Comments ?? "";
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

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công.", entity.MapToViewDto())
                : new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật thất bại.");
        }

        // ================== DELETE (soft) ==================
        public async Task<IServiceResult> DeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id && !e.IsDeleted
            );
            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy đánh giá.");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá đánh giá này.");

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá mềm thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá mềm thất bại.");
        }

        // ================== HARD DELETE ==================
        public async Task<IServiceResult> HardDeleteAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id
            );
            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy đánh giá.");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Bạn không có quyền xoá cứng đánh giá này.");

            // Chỉ Admin mới được xóa cứng
            if (!isAdmin)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ Admin mới có quyền xoá cứng đánh giá.");

            await _unitOfWork.ProcessingBatchEvaluationRepository.RemoveAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xoá cứng thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá cứng thất bại.");
        }

        // ================== BULK HARD DELETE ==================
        public async Task<IServiceResult> BulkHardDeleteAsync(List<Guid> ids, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            if (ids == null || !ids.Any())
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Danh sách ID không hợp lệ.");

            // Chỉ Admin mới được xóa cứng hàng loạt
            if (!isAdmin)
                return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ Admin mới có quyền xoá cứng hàng loạt đánh giá.");

            var entities = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => ids.Contains(e.EvaluationId)
            );

            if (!entities.Any())
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy đánh giá nào để xoá.");

            // Kiểm tra quyền cho từng entity
            foreach (var entity in entities)
            {
                var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
                if (!canAccess)
                    return new ServiceResult(Const.FAIL_DELETE_CODE, $"Bạn không có quyền xoá đánh giá {entity.EvaluationId}.");
            }

            foreach (var entity in entities)
            {
                await _unitOfWork.ProcessingBatchEvaluationRepository.RemoveAsync(entity);
            }

            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_DELETE_CODE, $"Đã xoá cứng {saved} đánh giá thành công.")
                : new ServiceResult(Const.FAIL_DELETE_CODE, "Xoá cứng hàng loạt thất bại.");
        }

        // ================== RESTORE ==================
        public async Task<IServiceResult> RestoreAsync(Guid id, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var entity = await _unitOfWork.ProcessingBatchEvaluationRepository.GetByIdAsync(
                e => e.EvaluationId == id && e.IsDeleted
            );
            if (entity == null)
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy đánh giá đã xóa.");

            var canAccess = await HasPermissionToAccessAsync(entity.BatchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Bạn không có quyền khôi phục đánh giá này.");

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ProcessingBatchEvaluationRepository.UpdateAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0
                ? new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Khôi phục đánh giá thành công.", entity.MapToViewDto())
                : new ServiceResult(Const.FAIL_UPDATE_CODE, "Khôi phục đánh giá thất bại.");
        }

        // ================== GET ALL ==================
        public async Task<IServiceResult> GetAllAsync(Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            // Chỉ Admin, Manager, Expert mới có quyền xem tất cả evaluations
            if (!isAdmin && !isManager && !isExpert)
                return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xem tất cả đánh giá.");

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
                return new ServiceResult(Const.FAIL_READ_CODE, "Không có quyền xem.");

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
                return new ServiceResult(Const.FAIL_READ_CODE, "Không có quyền xem.");

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
                return new ServiceResult(Const.FAIL_READ_CODE, "Không có quyền xem.");

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
    }
}



