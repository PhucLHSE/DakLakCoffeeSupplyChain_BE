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
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");
            var isExpert = User.IsInRole("AgriculturalExpert");
            var isManagerOrExpert = isManager || isExpert;

            var result = await _evaluationService.DeleteAsync(id, userId, isAdmin, isManagerOrExpert, isExpert);

            if (result.Status == Const.SUCCESS_DELETE_CODE) return Ok(result.Message);
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);
            if (result.Status == Const.FAIL_DELETE_CODE) return BadRequest(result.Message);
            if (result.Status == Const.ERROR_EXCEPTION) return Forbid();
            return StatusCode(500, result.Message);
        }
    }
}
