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
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService
                .GetAllCrops(userId, userRole);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(new { message = "Lấy danh sách vùng trồng thành công", data = result.Data });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
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
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService.GetCropById(cropId, userId, userRole);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(new { message = "Lấy thông tin vùng trồng thành công", data = result.Data });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // POST: api/<CropsController>
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CreateCropAsync([FromForm] CropCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = errors });
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
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService.CreateCrop(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                var cropData = (CropViewAllDto)result.Data;
                List<string> imageUrls = new List<string>();

                // Chỉ xử lý images
                var allMediaFiles = new List<IFormFile>();
                if (dto.Images?.Any() == true)
                    allMediaFiles.AddRange(dto.Images);

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

                        // Chỉ lấy URLs của images
                        imageUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                    }
                    catch (Exception ex)
                    {
                        // Log error nhưng vẫn trả về success với crop đã tạo
                        // Frontend có thể upload media sau
                    }
                }

                // Luôn fetch crop details với media files
                var cropDetailsResult = await _cropService.GetCropById(cropData.CropId, userId, userRole);
                if (cropDetailsResult.Status == Const.SUCCESS_READ_CODE)
                {
                    return Ok(new { 
                        message = "Tạo vùng trồng thành công",
                        crop = cropDetailsResult.Data,
                        uploadedFiles = allMediaFiles.Count,
                        imageUrls = imageUrls
                    });
                }

                return Ok(new { message = "Tạo vùng trồng thành công", data = result.Data });
            }

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // PUT: api/<CropsController>/{cropId}
        [HttpPut("{cropId}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> UpdateCropAsync(Guid cropId, [FromForm] CropUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = errors });
            }

            if (cropId != dto.CropId)
            {
                return BadRequest(new { message = "Crop ID trong URL không khớp với Crop ID trong body." });
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
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            // Kiểm tra quyền: chỉ Farmer sở hữu crop mới được update
            if (userRole != "Admin" && userRole != "Farmer")
            {
                return Forbid("Không có quyền cập nhật crop.");
            }

            var result = await _cropService.UpdateCrop(dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
            {
                var cropData = (CropViewAllDto)result.Data;
                List<string> imageUrls = new List<string>();

                // Xử lý images nếu có
                if (dto.Images?.Any() == true)
                {
                    try
                    {
                        var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                            files: dto.Images,
                            relatedEntity: "Crop",
                            relatedId: cropData.CropId,
                            uploadedBy: userId.ToString()
                        );

                        imageUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                    }
                    catch (Exception ex)
                    {
                        // Log error nhưng vẫn trả về success với crop đã cập nhật
                    }
                }

                return Ok(new { 
                    message = "Cập nhật vùng trồng thành công", 
                    data = result.Data,
                    uploadedImages = imageUrls.Count,
                    imageUrls = imageUrls
                });
            }

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
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
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService.SoftDeleteCrop(cropId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
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
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService.HardDeleteCrop(cropId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // PUT: api/crops/{cropId}/approve
        [HttpPut("{cropId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCropAsync(Guid cropId, [FromBody] CropApproveDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = errors });
            }

            Guid adminUserId;

            try
            {
                adminUserId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService.ApproveCropAsync(cropId, dto, adminUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = "Duyệt vùng trồng thành công", data = result.Data });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // PUT: api/crops/{cropId}/reject
        [HttpPut("{cropId}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectCropAsync(Guid cropId, [FromBody] CropRejectDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors = errors });
            }

            Guid adminUserId;

            try
            {
                adminUserId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(new { message = ERROR_USER_ID_NOT_FOUND_MSG });
            }

            var result = await _cropService.RejectCropAsync(cropId, dto, adminUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = "Từ chối vùng trồng thành công", data = result.Data });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }
    }
}
