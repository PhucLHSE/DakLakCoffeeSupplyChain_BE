using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.MediaDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CropsController : ControllerBase
    {
        private readonly ICropService _cropService;
        private readonly IMediaService _mediaService;
        private const string ERROR_USER_ID_NOT_FOUND_MSG = "Không xác định được userId từ token.";

        public CropsController(ICropService cropService, IMediaService mediaService)
        {
            _cropService = cropService;
            _mediaService = mediaService;
        }

        // GET: api/<CropsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetAllCropsAsync()
        {
            Guid userId;
            string userRole;

            try
            {
                // Lấy userId và userRole từ token qua ClaimsHelper
                userId = User.GetUserId();
                userRole = User.GetRole();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService
                .GetAllCrops(userId, userRole);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/<CropsController>/{cropId}
        [HttpGet("{cropId}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetCropByIdAsync(Guid cropId)
        {
            Guid userId;
            string userRole;

            try
            {
                userId = User.GetUserId();
                userRole = User.GetRole();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.GetCropById(cropId, userId, userRole);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // POST: api/<CropsController>
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CreateCropAsync([FromForm] CropCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid userId;
            string userRole;

            try
            {
                userId = User.GetUserId();
                userRole = User.GetRole();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.CreateCrop(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                var cropData = (CropViewAllDto)result.Data;
                List<string> imageUrls = new List<string>();
                List<string> videoUrls = new List<string>();
                List<string> documentUrls = new List<string>();

                // Gộp images, videos và documents
                var allMediaFiles = new List<IFormFile>();
                if (dto.Images?.Any() == true)
                    allMediaFiles.AddRange(dto.Images);
                if (dto.Videos?.Any() == true)
                    allMediaFiles.AddRange(dto.Videos);
                if (dto.Documents?.Any() == true)
                    allMediaFiles.AddRange(dto.Documents);

                if (allMediaFiles.Any())
                {
                    try
                    {
                        var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                            allMediaFiles,
                            relatedEntity: "Crop",
                            relatedId: cropData.CropId,
                            uploadedBy: userId.ToString()
                        );

                        // Lấy tất cả URLs của mỗi loại media
                        imageUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                        videoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                        // Documents sẽ được xử lý sau
                    }
                    catch (Exception ex)
                    {
                        // Log error silently
                    }
                }

                // Luôn fetch crop details với media files
                var cropDetailsResult = await _cropService.GetCropById(cropData.CropId, userId, userRole);
                if (cropDetailsResult.Status == Const.SUCCESS_READ_CODE)
                {
                    return Ok(new { 
                        crop = cropDetailsResult.Data,
                        uploadedFiles = allMediaFiles.Count,
                        imageUrls = imageUrls,
                        videoUrls = videoUrls,
                        documentUrls = documentUrls
                    });
                }

                return Ok(result.Data);
            }

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/<CropsController>/{cropId}
        [HttpPut("{cropId}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> UpdateCropAsync(Guid cropId, [FromForm] CropUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (cropId != dto.CropId)
            {
                return BadRequest("Crop ID trong URL không khớp với Crop ID trong body.");
            }

            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.UpdateCrop(dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
            {
                var cropData = (CropViewAllDto)result.Data;
                List<string> imageUrls = new List<string>();
                List<string> videoUrls = new List<string>();
                List<string> documentUrls = new List<string>();

                // Xử lý media files nếu có
                var allMediaFiles = new List<IFormFile>();
                if (dto.Images?.Any() == true)
                    allMediaFiles.AddRange(dto.Images);
                if (dto.Videos?.Any() == true)
                    allMediaFiles.AddRange(dto.Videos);
                if (dto.Documents?.Any() == true)
                    allMediaFiles.AddRange(dto.Documents);

                if (allMediaFiles.Any())
                {
                    try
                    {
                        var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                            allMediaFiles,
                            relatedEntity: "Crop",
                            relatedId: cropData.CropId,
                            uploadedBy: userId.ToString()
                        );

                        // Lấy URLs của media mới
                        imageUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                        videoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                        documentUrls = mediaList.Where(m => m.MediaType == "document").Select(m => m.MediaUrl).ToList();
                    }
                    catch (Exception ex)
                    {
                        // Log error silently
                    }
                }

                // Fetch updated crop details với media files
                string userRole = User.GetRole();
                var cropDetailsResult = await _cropService.GetCropById(cropData.CropId, userId, userRole);
                if (cropDetailsResult.Status == Const.SUCCESS_READ_CODE)
                {
                    return Ok(new { 
                        crop = cropDetailsResult.Data,
                        uploadedFiles = allMediaFiles.Count,
                        imageUrls = imageUrls,
                        videoUrls = videoUrls,
                        documentUrls = documentUrls
                    });
                }

                return Ok(result.Data);
            }

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // DELETE: api/<CropsController>/{cropId}/soft
        [HttpDelete("{cropId}/softDelete")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> SoftDeleteCropAsync(Guid cropId)
        {
            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.SoftDeleteCrop(cropId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // DELETE: api/<CropsController>/{cropId}/hard
        [HttpDelete("{cropId}/hardDelete")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> HardDeleteCropAsync(Guid cropId)
        {
            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.HardDeleteCrop(cropId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/crops/{cropId}/approve
        [HttpPut("{cropId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCropAsync(Guid cropId, [FromBody] CropApproveDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid adminUserId;

            try
            {
                adminUserId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.ApproveCropAsync(cropId, dto, adminUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/crops/{cropId}/reject
        [HttpPut("{cropId}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectCropAsync(Guid cropId, [FromBody] CropRejectDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid adminUserId;

            try
            {
                adminUserId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.RejectCropAsync(cropId, dto, adminUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
