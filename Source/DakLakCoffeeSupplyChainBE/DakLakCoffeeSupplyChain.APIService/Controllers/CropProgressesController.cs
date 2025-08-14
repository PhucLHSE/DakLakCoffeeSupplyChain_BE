using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,BusinessManager,Farmer,Expert")]
    public class CropProgressesController : ControllerBase
    {
        private readonly ICropProgressService _cropProgressService;
        private readonly IMediaService _mediaService;

        public CropProgressesController(
            ICropProgressService cropProgressService,
            IMediaService mediaService)
        {
            _cropProgressService = cropProgressService;
            _mediaService = mediaService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllCropProgressesAsync()
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("by-detail/{cropSeasonDetailId}")]
        public async Task<IActionResult> GetByCropSeasonDetailId(Guid cropSeasonDetailId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = role == "Admin";
            bool isManager = role == "BusinessManager";

            var result = await _cropProgressService
                .GetByCropSeasonDetailId(cropSeasonDetailId, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CropProgressCreateWithMediaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                Guid userId;
                try { userId = User.GetUserId(); }
                catch { return Unauthorized("Không xác định được userId từ token."); }

                // Tạo DTO từ request
                var dto = new CropProgressCreateDto
                {
                    CropSeasonDetailId = request.CropSeasonDetailId,
                    StageId = request.StageId,
                    ProgressDate = request.ProgressDate,
                    ActualYield = request.ActualYield,
                    Note = request.Notes,
                    // StageDescription sẽ được tự động lấy từ CropStage.Description
                    PhotoUrl = string.Empty, // Sẽ được cập nhật sau khi upload media
                    VideoUrl = string.Empty, // Sẽ được cập nhật sau khi upload media
                    StepIndex = request.StageId // Tạm thời dùng StageId, sẽ được cập nhật thành OrderIndex sau
                };

                // Tạo progress trước
                var result = await _cropProgressService.Create(dto, userId);

                if (result.Status != Const.SUCCESS_CREATE_CODE)
                    return BadRequest(new { message = result.Message });

                if (result.Data is not CropProgressViewDetailsDto createdProgress)
                    return StatusCode(500, new { message = "Không lấy được progress sau khi tạo." });

                string? photoUrl = null, videoUrl = null;

                // Nếu có media files thì upload
                if (request.MediaFiles?.Any() == true)
                {
                    var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                        request.MediaFiles,
                        relatedEntity: "CropProgress",
                        relatedId: createdProgress.ProgressId,
                        uploadedBy: userId.ToString()
                    );

                    photoUrl = mediaList.FirstOrDefault(m => m.MediaType == "image")?.MediaUrl;
                    videoUrl = mediaList.FirstOrDefault(m => m.MediaType == "video")?.MediaUrl;

                    // Cập nhật media URLs vào progress
                    await _cropProgressService.UpdateMediaUrlsAsync(createdProgress.ProgressId, photoUrl, videoUrl);
                }

                // Trả về kết quả hoàn chỉnh
                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = result.Message,
                    progress = createdProgress,
                    photoUrl,
                    videoUrl
                });
            }
            catch (Exception ex)
            {
                var fullMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống: " + fullMessage });
            }
        }

        [HttpPut("{progressId}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Update(Guid progressId, [FromBody] CropProgressUpdateDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Phải cung cấp dữ liệu cập nhật.");

                // Validate progressId
                if (progressId != dto.ProgressId)
                    return BadRequest("ProgressId trong route không khớp với nội dung.");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                Guid userId;
                try { userId = User.GetUserId(); }
                catch { return Unauthorized("Không xác định được userId từ token."); }

                // Update progress
                var result = await _cropProgressService.Update(dto, userId);

                if (result.Status != Const.SUCCESS_UPDATE_CODE)
                    return BadRequest(new { message = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                var fullMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống: " + fullMessage });
            }
        }

        [HttpPatch("soft-delete/{progressId}")]
        public async Task<IActionResult> SoftDeleteById(Guid progressId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .SoftDeleteById(progressId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return NotFound(result.Message);
        }

        [HttpDelete("hard/{progressId}")]
        public async Task<IActionResult> HardDelete(Guid progressId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .DeleteById(progressId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return NotFound(result.Message);
        }
    }
}