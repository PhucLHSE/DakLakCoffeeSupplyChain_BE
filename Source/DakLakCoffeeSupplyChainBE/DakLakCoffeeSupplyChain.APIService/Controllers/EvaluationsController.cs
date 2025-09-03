using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvaluationsController : ControllerBase
    {
        private readonly IEvaluationService _evaluationService;
        private readonly IUnitOfWork _unitOfWork;
        
        public EvaluationsController(IEvaluationService evaluationService, IUnitOfWork unitOfWork)
        {
            _evaluationService = evaluationService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.GetAllAsync(userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpGet("by-batch/{batchId:guid}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> GetByBatch(Guid batchId)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.GetByBatchAsync(batchId, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpGet("summary/{batchId:guid}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> GetSummary(Guid batchId)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.GetSummaryByBatchAsync(batchId, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        // 🔧 MỚI: API để lấy thông tin về các stage cần cập nhật khi retry
        [HttpGet("failed-stages/{batchId:guid}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> GetFailedStages(Guid batchId)
        {
            try
            {
                var failedStages = await _evaluationService.GetFailedStagesForBatchAsync(batchId);
                var failedStagesDto = failedStages.Select(s => new { 
                    stageId = s.StageId, 
                    stageName = s.StageName, 
                    orderIndex = s.OrderIndex 
                }).ToList();
                return Ok(new { failedStages = failedStagesDto });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi lấy thông tin failed stages: {ex.Message}");
                return StatusCode(500, "Lỗi khi lấy thông tin stage cần cập nhật");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> Create([FromBody] EvaluationCreateDto dto)
        {
            // Log incoming data để debug
            Console.WriteLine($"🔍 DEBUG: Received evaluation data:");
            Console.WriteLine($"  BatchId: {dto.BatchId}");
            Console.WriteLine($"  EvaluationResult: {dto.EvaluationResult}");
            Console.WriteLine($"  Comments: {dto.Comments}");
            Console.WriteLine($"  EvaluatedAt: {dto.EvaluatedAt}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"❌ ModelState validation failed:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"  - {error.ErrorMessage}");
                }
                return BadRequest(new { 
                    message = "Dữ liệu không hợp lệ", 
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() 
                });
            }

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            // Validate EvaluationResult trước khi gửi đến service
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary" };
            var evaluationResult = dto.EvaluationResult ?? "";
            if (Array.FindIndex(validResults, x => string.Equals(x, evaluationResult, StringComparison.OrdinalIgnoreCase)) == -1)
                return BadRequest("Kết quả đánh giá không hợp lệ. Chỉ chấp nhận: Pass, Fail, NeedsImprovement, Temporary.");

            Console.WriteLine($"🔍 DEBUG: Calling service with userId: {userId}, isAdmin: {isAdmin}, isManagerOrExpert: {isManagerOrExpert}, isExpert: {isExpert}");
            var result = await _evaluationService.CreateAsync(dto, userId, isAdmin, isManagerOrExpert, isExpert);
            
            Console.WriteLine($"🔍 DEBUG: Service returned - Status: {result.Status}, Message: {result.Message}");
            Console.WriteLine($"🔍 DEBUG: Expected SUCCESS_CREATE_CODE: {Const.SUCCESS_CREATE_CODE}");

            if (result.Status == Const.SUCCESS_CREATE_CODE) 
            {
                // Thêm thông tin workflow vào response
                var response = new
                {
                    data = result.Data,
                    message = result.Message,
                    workflow = new
                    {
                        batchStatusUpdated = evaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Completed" : 
                                            evaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) ? "InProgress" : "Unchanged"
                    }
                };
                return Ok(response);
            }
            if (result.Status == Const.FAIL_CREATE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EvaluationUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            // Validate EvaluationResult trước khi gửi đến service
            var validResults = new[] { "Pass", "Fail", "NeedsImprovement", "Temporary" };
            var evaluationResult = dto.EvaluationResult ?? "";
            if (Array.FindIndex(validResults, x => string.Equals(x, evaluationResult, StringComparison.OrdinalIgnoreCase)) == -1)
                return BadRequest("Kết quả đánh giá không hợp lệ. Chỉ chấp nhận: Pass, Fail, NeedsImprovement, Temporary.");

            var result = await _evaluationService.UpdateAsync(id, dto, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_UPDATE_CODE) 
            {
                // Thêm thông tin workflow vào response
                var response = new
                {
                    data = result.Data,
                    message = result.Message,
                    workflow = new
                    {
                        batchStatusUpdated = evaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Completed" : 
                                            evaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) ? "InProgress" : "Unchanged"
                    }
                };
                return Ok(response);
            }
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.FAIL_UPDATE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Validate input
            if (id == Guid.Empty)
                return BadRequest("ID đánh giá không hợp lệ.");

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.DeleteAsync(id, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_DELETE_CODE) 
            {
                // Thêm thông tin chi tiết vào response
                var response = new
                {
                    message = result.Message,
                    deletedAt = DateTime.UtcNow,
                    deletedBy = userId,
                    evaluationId = id,
                    note = "Đánh giá đã được xóa mềm (soft delete). Dữ liệu vẫn được lưu trong database."
                };
                return Ok(response);
            }
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.FAIL_DELETE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpDelete("{id:guid}/hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            // Validate input
            if (id == Guid.Empty)
                return BadRequest("ID đánh giá không hợp lệ.");

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.HardDeleteAsync(id, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_DELETE_CODE) 
            {
                // Thêm thông tin chi tiết vào response
                var response = new
                {
                    message = result.Message,
                    deletedAt = DateTime.UtcNow,
                    deletedBy = userId,
                    evaluationId = id,
                    note = "Đánh giá đã được xóa cứng (hard delete). Dữ liệu đã bị xóa hoàn toàn khỏi database.",
                    warning = "⚠️ Hành động này không thể hoàn tác!"
                };
                return Ok(response);
            }
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.FAIL_DELETE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpDelete("bulk-hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkHardDelete([FromBody] List<Guid> ids)
        {
            // Validate input
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách ID không hợp lệ.");

            if (ids.Count > 100)
                return BadRequest("Chỉ có thể xóa tối đa 100 đánh giá cùng lúc.");

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.BulkHardDeleteAsync(ids, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_DELETE_CODE) 
            {
                // Thêm thông tin chi tiết vào response
                var response = new
                {
                    message = result.Message,
                    deletedAt = DateTime.UtcNow,
                    deletedBy = userId,
                    deletedCount = ids.Count,
                    note = "Các đánh giá đã được xóa cứng (hard delete). Dữ liệu đã bị xóa hoàn toàn khỏi database.",
                    warning = "⚠️ Hành động này không thể hoàn tác!"
                };
                return Ok(response);
            }
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.FAIL_DELETE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpPatch("{id:guid}/restore")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> Restore(Guid id)
        {
            // Validate input
            if (id == Guid.Empty)
                return BadRequest("ID đánh giá không hợp lệ.");

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.RestoreAsync(id, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_UPDATE_CODE) 
            {
                // Thêm thông tin chi tiết vào response
                var response = new
                {
                    data = result.Data,
                    message = result.Message,
                    restoredAt = DateTime.UtcNow,
                    restoredBy = userId,
                    evaluationId = id,
                    note = "Đánh giá đã được khôi phục thành công."
                };
                return Ok(response);
            }
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.FAIL_UPDATE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        [HttpGet("deleted/{batchId:guid}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> GetDeleted(Guid batchId)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.GetDeletedByBatchAsync(batchId, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy thông tin failure và stages cần retry khi batch bị đánh giá FAIL
        /// </summary>
        /// <param name="batchId">ID của batch</param>
        /// <returns>Thông tin failure và stages đã thực hiện để retry</returns>
        [HttpGet("failure-info/{batchId:guid}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,Farmer")]
        public async Task<IActionResult> GetFailureInfo(Guid batchId)
        {
            try
            {
                // Lấy evaluation cuối cùng của batch
                var latestEvaluation = await _unitOfWork.ProcessingBatchEvaluationRepository.GetAllAsync(
                    e => e.BatchId == batchId && !e.IsDeleted,
                    q => q.OrderByDescending(e => e.CreatedAt)
                );

                var evaluation = latestEvaluation.FirstOrDefault();
                if (evaluation == null)
                {
                        return NotFound(new
                        {
                            status = Const.WARNING_NO_DATA_CODE,
                        message = "Không tìm thấy đánh giá cho batch này",
                        data = new object()
                    });
                }

                if (evaluation.EvaluationResult != "Fail")
                {
                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                        message = "Batch này không bị đánh giá FAIL",
                        data = new
                        {
                            evaluationResult = evaluation.EvaluationResult,
                            note = "Chỉ hiển thị thông tin retry khi batch bị FAIL"
                        }
                    });
                }

                // Lấy batch để biết method
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                {
                    return NotFound(new
                    {
                        status = Const.WARNING_NO_DATA_CODE,
                        message = "Không tìm thấy batch",
                        data = new object()
                    });
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

                // Tạo response data
                var failureInfo = new
                {
                    batchId = batchId,
                    evaluationId = evaluation.EvaluationId,
                    failedAt = evaluation.CreatedAt,
                    comments = evaluation.Comments,
                    // Thông tin stage cần retry
                    failedStage = stageToRetry != null ? new
                    {
                        stageId = stageToRetry.StageId,
                        stageName = stageToRetry.StageName,
                        orderIndex = stageToRetry.OrderIndex,
                        lastStepIndex = lastProgress?.StepIndex ?? 0
                    } : null,
                    // Thông tin tất cả stages đã thực hiện
                    completedStages = progresses.Select(p => new
                    {
                        stageId = p.StageId,
                        stageName = stages.FirstOrDefault(s => s.StageId == p.StageId)?.StageName,
                        orderIndex = stages.FirstOrDefault(s => s.StageId == p.StageId)?.OrderIndex,
                        stepIndex = p.StepIndex,
                        outputQuantity = p.OutputQuantity,
                        outputUnit = p.OutputUnit,
                        progressDate = p.ProgressDate
                    }).ToList(),
                    note = "Batch bị fail - cần retry stage cuối cùng đã thực hiện"
                };
                
                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy thông tin failure thành công",
                    data = failureInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy thông tin failure: {ex.Message}",
                    data = new object()
                });
            }
        }


    }
}

