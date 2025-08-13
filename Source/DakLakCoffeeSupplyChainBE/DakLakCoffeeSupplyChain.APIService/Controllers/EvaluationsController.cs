using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
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
        public EvaluationsController(IEvaluationService evaluationService)
        {
            _evaluationService = evaluationService;
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

        [HttpPost]
        [Authorize(Roles = "Farmer,Admin,BusinessManager,AgriculturalExpert")]
        public async Task<IActionResult> Create([FromBody] EvaluationCreateDto dto)
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
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Kết quả đánh giá không hợp lệ. Chỉ chấp nhận: Pass, Fail, NeedsImprovement, Temporary.");

            var result = await _evaluationService.CreateAsync(dto, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_CREATE_CODE) 
            {
                // Thêm thông tin workflow vào response
                var response = new
                {
                    data = result.Data,
                    message = result.Message,
                    workflow = new
                    {
                        batchStatusUpdated = dto.EvaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Completed" : 
                                            dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) ? "InProgress" : "Unchanged"
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
            if (!validResults.Contains(dto.EvaluationResult, StringComparer.OrdinalIgnoreCase))
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
                        batchStatusUpdated = dto.EvaluationResult.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Completed" : 
                                            dto.EvaluationResult.Equals("Fail", StringComparison.OrdinalIgnoreCase) ? "InProgress" : "Unchanged"
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
    }
}
