using DakLakCoffeeSupplyChain.APIService.Requests.ProcessingBatchProgressReques;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingBatchsProgressController : ControllerBase
    {
        private readonly IProcessingBatchProgressService _processingBatchProgressService;
        private readonly IUploadService _uploadService;

        public ProcessingBatchsProgressController(IProcessingBatchProgressService processingBatchProgressService, IUploadService uploadService)
        {
            _processingBatchProgressService = processingBatchProgressService;
            _uploadService = uploadService;
        }

        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst("userId")?.Value
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return BadRequest("Không thể lấy userId từ token.");
            }

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService.GetAllByUserIdAsync(userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpGet("detail/{id}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _processingBatchProgressService.GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost("{batchId}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> Create(Guid batchId, [FromBody] ProcessingBatchProgressCreateDto input)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService.CreateAsync(batchId, input, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(StatusCodes.Status201Created, result.Message);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpPost("{batchId}/upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> CreateWithMedia(Guid batchId, [FromForm] ProcessingBatchProgressCreateRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                string? photoUrl = null;
                string? videoUrl = null;

                if (request.PhotoFile != null)
                {
                    var photoResult = await _uploadService.UploadImageAsync(request.PhotoFile);
                    photoUrl = photoResult?.Url;
                }

                if (request.VideoFile != null)
                {
                    var videoResult = await _uploadService.UploadVideoAsync(request.VideoFile);
                    videoUrl = videoResult?.Url;
                }

                var dto = new ProcessingBatchProgressCreateDto
                {
                    ProgressDate = request.ProgressDate,
                    OutputQuantity = request.OutputQuantity,
                    OutputUnit = request.OutputUnit,
                    PhotoUrl = photoUrl,
                    VideoUrl = videoUrl
                };

                var result = await _processingBatchProgressService.CreateAsync(batchId, dto, userId, isAdmin, isManager);

                if (result.Status == Const.SUCCESS_CREATE_CODE)
                    return StatusCode(StatusCodes.Status201Created, new { message = result.Message });

                if (result.Status == Const.FAIL_CREATE_CODE)
                    return BadRequest(new { message = result.Message });

                return StatusCode(500, new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProcessingBatchProgressUpdateDto input)
        {
            var result = await _processingBatchProgressService.UpdateAsync(id, input);

          
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data ?? new { });

            
            if (result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                return BadRequest(result);

            return StatusCode(500, result); // fallback lỗi hệ thống
        }
        [HttpPatch("{id}/soft-delete")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _processingBatchProgressService.SoftDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpDelete("hard/{id}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _processingBatchProgressService.HardDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpPost("{batchId}/advance")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> AdvanceToNextStepWithFile(
        Guid batchId,
        [FromForm] AdvanceProcessingBatchProgressRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                string? photoUrl = null;
                string? videoUrl = null;

                if (request.PhotoFile != null)
                {
                    var uploadResult = await _uploadService.UploadImageAsync(request.PhotoFile);
                    photoUrl = uploadResult?.Url;
                }

                if (request.VideoFile != null)
                {
                    var uploadResult = await _uploadService.UploadVideoAsync(request.VideoFile);
                    videoUrl = uploadResult?.Url;
                }

                var dto = new AdvanceProcessingBatchProgressDto
                {
                    ProgressDate = request.ProgressDate,
                    OutputQuantity = request.OutputQuantity,
                    OutputUnit = request.OutputUnit,
                    PhotoUrl = photoUrl,
                    VideoUrl = videoUrl
                };

                var result = await _processingBatchProgressService.AdvanceProgressByBatchIdAsync(batchId, dto, userId, isAdmin, isManager);

                if (result.Status == Const.SUCCESS_CREATE_CODE || result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(new { message = result.Message });

                if (result.Status == Const.FAIL_CREATE_CODE || result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                    return BadRequest(new { message = result.Message });

                return StatusCode(500, new { message = result.Message });
            }
            catch (Exception ex)
            {
                // Trả lỗi rõ ràng về FE
                return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
            }
        }

    }
}
