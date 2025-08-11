using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
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

        public EvaluationService(
            IUnitOfWork unitOfWork, 
            ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = codeGenerator;
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
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary" };
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Kết quả đánh giá không hợp lệ. Chỉ chấp nhận: Pass, Fail, NeedsImprovement, Temporary.");

            // Kiểm tra batch status trước khi đánh giá
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(dto.BatchId);
            if (batch == null)
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy lô sơ chế.");

            // Chỉ cho phép đánh giá khi batch đã hoàn thành hoặc đang xử lý
            if (batch.Status != "Completed" && batch.Status != "InProgress")
                return new ServiceResult(Const.FAIL_CREATE_CODE, "Chỉ có thể đánh giá lô đã hoàn thành hoặc đang xử lý.");

            var code = await _codeGenerator.GenerateEvaluationCodeAsync(DateTime.UtcNow.Year);

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

            var entity = new ProcessingBatchEvaluation
            {
                EvaluationId = Guid.NewGuid(),
                EvaluationCode = code,
                BatchId = dto.BatchId,
                EvaluatedBy = userId,
                EvaluationResult = dto.EvaluationResult,
                Comments = detailedComments.Trim(),
                EvaluatedAt = dto.EvaluatedAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.ProcessingBatchEvaluationRepository.CreateAsync(entity);

            // Xử lý logic workflow theo kết quả đánh giá
            if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu đánh giá Fail, chuyển batch về trạng thái InProgress để farmer sửa
                if (batch.Status == "Completed" || batch.Status == "AwaitingEvaluation")
                {
                    batch.Status = "InProgress";
                    batch.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
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

            // Xử lý logic workflow nếu kết quả thay đổi
            if (!oldResult.Equals(dto.EvaluationResult, StringComparison.OrdinalIgnoreCase))
            {
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(entity.BatchId);
                
                if (batch != null)
                {
                    if (dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase))
                    {
                                                 // Nếu đánh giá Fail, chuyển batch về trạng thái InProgress
                         if (batch.Status == "Completed" || batch.Status == "AwaitingEvaluation")
                         {
                             batch.Status = "InProgress";
                             batch.UpdatedAt = DateTime.UtcNow;
                             await _unitOfWork.ProcessingBatchRepository.UpdateAsync(batch);
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
                }
            }

            var saved = await _unitOfWork.SaveChangesAsync();

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

        // ================== LIST BY BATCH ==================
        public async Task<IServiceResult> GetByBatchAsync(Guid batchId, Guid userId, bool isAdmin, bool isManager, bool isExpert)
        {
            var canAccess = await HasPermissionToAccessAsync(batchId, userId, isAdmin, isManager, isExpert);
            if (!canAccess)
                return new ServiceResult(Const.FAIL_READ_CODE, "Không có quyền xem.");

            var list = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                e => !e.IsDeleted && e.BatchId == batchId,
                orderBy: q => q.OrderByDescending(x => x.EvaluatedAt).ThenByDescending(x => x.CreatedAt),
                asNoTracking: true
            );

            var dtos = list.Select(x => x.MapToViewDto()).ToList();
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



