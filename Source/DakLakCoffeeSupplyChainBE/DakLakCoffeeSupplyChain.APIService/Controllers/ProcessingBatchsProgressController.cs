using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingBatchsProgressController : ControllerBase
    {
        private readonly IProcessingBatchProgressService _processingBatchProgressService;
        private readonly IUploadService _uploadService;
        private readonly IMediaService _mediaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProcessingWasteService _processingWasteService;

        public ProcessingBatchsProgressController(
            IProcessingBatchProgressService processingBatchProgressService, 
            IUploadService uploadService, 
            IMediaService mediaService,
            IUnitOfWork unitOfWork,
            IProcessingWasteService processingWasteService)
        {
            _processingBatchProgressService = processingBatchProgressService;
            _uploadService = uploadService;
            _mediaService = mediaService;
            _unitOfWork = unitOfWork;
            _processingWasteService = processingWasteService;
        }

        [HttpGet("available-batches")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> GetAvailableBatchesForProgress()
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService
                .GetAvailableBatchesForProgressAsync(userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("check-batch/{batchId}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> CheckBatchCanCreateProgress(Guid batchId)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService
                .CheckBatchCanCreateProgressAsync(batchId, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return Forbid(result.Message);

            return StatusCode(500, result.Message);
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

            var result = await _processingBatchProgressService
                .GetAllByUserIdAsync(userId, isAdmin, isManager);

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
            var result = await _processingBatchProgressService
                .GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("batch/{batchId}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> GetByBatchId(Guid batchId)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService
                .GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("progress/{progressId}/media")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> GetProgressMedia(Guid progressId)
        {
            try
            {
                var mediaFiles = await _mediaService.GetMediaByRelatedAsync("ProcessingProgress", progressId);
                
                var photoUrls = mediaFiles.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                var videoUrls = mediaFiles.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                
                return Ok(new
                {
                    progressId,
                    photoUrls,
                    videoUrls,
                    totalCount = mediaFiles.Count,
                    message = "Lấy media thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi lấy media: {ex.Message}" });
            }
        }

        [HttpGet("debug-advance/{batchId}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> DebugAdvance(Guid batchId)
            {
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService
                .DebugAdvanceAsync(batchId, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }



        [HttpPost("{batchId}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> Create(
            Guid batchId, 
            [FromBody] ProcessingBatchProgressCreateDto input)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService
                .CreateAsync(batchId, input, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(StatusCodes.Status201Created, result.Message);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost("{batchId}/upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> CreateWithMedia(
            Guid batchId, 
            [FromForm] ProcessingBatchProgressCreateRequest request)
        {
            try
            {
                // Debug log để kiểm tra request
                Console.WriteLine($"🔍 Backend: Received request for batchId: {batchId}");
                Console.WriteLine($"🔍 Backend: Request Wastes count: {request.Wastes?.Count ?? 0}");
                Console.WriteLine($"🔍 Backend: Request.Form.Keys:");
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"  - {key}: {Request.Form[key]}");
                }
                if (request.Wastes?.Any() == true)
                {
                    foreach (var waste in request.Wastes)
                    {
                        Console.WriteLine($"🔍 Backend: Waste - Type: {waste.WasteType}, Quantity: {waste.Quantity}, Unit: {waste.Unit}");
                    }
                }
                else
                {
                    Console.WriteLine($"🔍 Backend: No wastes found in request.Wastes");
                }
                
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");

                var isManager = User.IsInRole("BusinessManager");

                // Gọi service để tạo progress với waste
                var result = await _processingBatchProgressService
                    .CreateWithMediaAndWasteAsync(batchId, request, userId, isAdmin, isManager);

                if (result.Status != Const.SUCCESS_CREATE_CODE)
                    return BadRequest(new { message = result.Message });

                if (result.Data is not ProcessingBatchProgressMediaResponse response)
                    return StatusCode(500, new { message = "Không lấy được response từ service." });

                // Upload media nếu có
                string? photoUrl = null, videoUrl = null;
                List<string> photoUrls = new List<string>();
                List<string> videoUrls = new List<string>();

                var allMediaFiles = new List<IFormFile>();
                if (request.PhotoFiles?.Any() == true)
                    allMediaFiles.AddRange(request.PhotoFiles);
                if (request.VideoFiles?.Any() == true)
                    allMediaFiles.AddRange(request.VideoFiles);

                if (allMediaFiles.Any())
                {
                    try
                    {
                        var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                            allMediaFiles,
                            relatedEntity: "ProcessingProgress",
                            relatedId: response.ProgressId,
                            uploadedBy: userId.ToString()
                        );

                        // Lấy tất cả URLs của mỗi loại media
                        photoUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                        videoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                        
                        photoUrl = photoUrls.FirstOrDefault();
                        videoUrl = videoUrls.FirstOrDefault();

                        // Cập nhật response với media URLs
                        response.PhotoUrl = photoUrl;
                        response.VideoUrl = videoUrl;
                        response.MediaCount = allMediaFiles.Count;
                        response.AllPhotoUrls = photoUrls;
                        response.AllVideoUrls = videoUrls;
                    }
                    catch (Exception mediaEx)
                    {
                        // Log lỗi media nhưng không fail toàn bộ request
                        Console.WriteLine($"WARNING: Media upload failed but progress was created successfully: {mediaEx.Message}");
                    }
                }

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (Exception ex)
            {
                var fullMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống: " + fullMessage });
            }
        }


        [HttpPut("{id}")]
        [Authorize(Roles = "Farmer,Admin, BusinessManager")]
        public async Task<IActionResult> Update(
            Guid id, 
            [FromBody] ProcessingBatchProgressUpdateDto input)
        {
            var result = await _processingBatchProgressService
                .UpdateAsync(id, input);
          
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
            var result = await _processingBatchProgressService
                .SoftDeleteAsync(id);

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
            var result = await _processingBatchProgressService
                .HardDeleteAsync(id);

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
                // 🔍 DEBUG: Log chi tiết về waste data trong advance
                Console.WriteLine($"🔍 ADVANCE: Received advance request for batchId: {batchId}");
                Console.WriteLine($"🔍 ADVANCE: Request.Form.Keys:");
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"  - {key}: {Request.Form[key]}");
                }
                Console.WriteLine($"🔍 ADVANCE: Request Wastes count: {request.Wastes?.Count ?? 0}");
                if (request.Wastes?.Any() == true)
                {
                    foreach (var waste in request.Wastes)
                    {
                        Console.WriteLine($"🔍 ADVANCE: Waste from array - Type: {waste.WasteType}, Quantity: {waste.Quantity}, Unit: {waste.Unit}");
                    }
                }
                else
                {
                    Console.WriteLine($"🔍 ADVANCE: No wastes found in request.Wastes array");
                }
                
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                // Gọi service để advance progress với waste
                var result = await _processingBatchProgressService
                    .AdvanceWithMediaAndWasteAsync(batchId, request, userId, isAdmin, isManager);

                if (result.Status != Const.SUCCESS_CREATE_CODE && result.Status != Const.SUCCESS_UPDATE_CODE)
                {
                    if (result.Status == Const.FAIL_CREATE_CODE || result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                        return BadRequest(new { message = result.Message });
                    return StatusCode(500, new { message = result.Message });
                }

                if (result.Data is not ProcessingBatchProgressMediaResponse response)
                    return StatusCode(500, new { message = "Không lấy được response từ service." });

                // Upload media nếu có
                var allMediaFiles = new List<IFormFile>();
                if (request.PhotoFiles?.Any() == true)
                    allMediaFiles.AddRange(request.PhotoFiles);
                if (request.VideoFiles?.Any() == true)
                    allMediaFiles.AddRange(request.VideoFiles);

                if (allMediaFiles.Any())
                {
                    try
                    {
                        var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                            allMediaFiles,
                            relatedEntity: "ProcessingProgress",
                            relatedId: response.ProgressId,
                            uploadedBy: userId.ToString()
                        );

                        // Lấy tất cả URLs của mỗi loại media
                        var uploadedPhotoUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                        var uploadedVideoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                        
                        var uploadedPhotoUrl = uploadedPhotoUrls.FirstOrDefault();
                        var uploadedVideoUrl = uploadedVideoUrls.FirstOrDefault();

                        // Cập nhật response với media URLs
                        response.PhotoUrl = uploadedPhotoUrl;
                        response.VideoUrl = uploadedVideoUrl;
                        response.MediaCount = allMediaFiles.Count;
                        response.AllPhotoUrls = uploadedPhotoUrls;
                        response.AllVideoUrls = uploadedVideoUrls;
                    }
                    catch (Exception mediaEx)
                    {
                        // Log lỗi media nhưng không fail toàn bộ request
                        Console.WriteLine($"WARNING: Media upload failed but progress was created successfully: {mediaEx.Message}");
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Trả lỗi rõ ràng về FE
                return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpGet("{batchId}/retry-info")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> GetBatchInfoBeforeRetry(Guid batchId)
            {
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

            var result = await _processingBatchProgressService
                .GetBatchInfoBeforeRetryAsync(batchId, userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return Forbid(result.Message);

            return StatusCode(500, result.Message);
        }


        [HttpPost("{batchId}/update-after-evaluation")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> UpdateProgressAfterEvaluation(
            Guid batchId,
            [FromForm] ProcessingBatchProgressCreateRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                // Xử lý parameters sử dụng helper
                var parameters = ProcessingHelper.ProcessParameters(
                    request.ParameterName,
                    request.ParameterValue,
                    request.Unit,
                    request.RecordedAt,
                    request.ParametersJson
                );
                
                // Validate parameters
                if (!ProcessingHelper.ValidateParameters(parameters))
                {
                    return BadRequest(new { message = "Dữ liệu parameters không hợp lệ." });
                }

                // Tạo progress trước
                var dto = new ProcessingBatchProgressCreateDto
                {
                    StageId = request.StageId, // 🔧 MỚI: Truyền StageId từ frontend
                    ProgressDate = request.ProgressDate,
                    OutputQuantity = request.OutputQuantity,
                    OutputUnit = request.OutputUnit,
                    PhotoUrl = null, // Sẽ được cập nhật sau
                    VideoUrl = null, // Sẽ được cập nhật sau
                    Parameters = parameters.Any() ? parameters : null
                };

                var result = await _processingBatchProgressService
                    .UpdateProgressAfterEvaluationAsync(batchId, dto, userId, isAdmin, isManager);

                if (result.Status != Const.SUCCESS_CREATE_CODE)
                {
                    if (result.Status == Const.FAIL_CREATE_CODE || result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                        return BadRequest(new { message = result.Message });
                    return StatusCode(500, new { message = result.Message });
                }

                string? updatePhotoUrl = null;
                string? updateVideoUrl = null;
                List<string> updatePhotoUrls = new List<string>();
                List<string> updateVideoUrls = new List<string>();

                // Upload media sau khi tạo progress thành công
                var allMediaFiles = new List<IFormFile>();
                if (request.PhotoFiles?.Any() == true)
                    allMediaFiles.AddRange(request.PhotoFiles);
                if (request.VideoFiles?.Any() == true)
                    allMediaFiles.AddRange(request.VideoFiles);

                if (allMediaFiles.Any())
                {
                    try
                    {
                        // Lấy progressId từ result nếu có thể
                        var latestProgressForMedia = await _processingBatchProgressService.GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);
                        if (latestProgressForMedia.Status == Const.SUCCESS_READ_CODE && latestProgressForMedia.Data is List<ProcessingBatchProgressViewAllDto> progressesForMedia)
                        {
                            var latestProgressId = progressesForMedia.LastOrDefault()?.ProgressId;
                            if (latestProgressId.HasValue)
                            {
                                var mediaList = await _mediaService.UploadAndSaveMediaAsync(
                                    allMediaFiles,
                                    relatedEntity: "ProcessingProgress",
                                    relatedId: latestProgressId.Value,
                                    uploadedBy: userId.ToString()
                                );

                                // Lấy tất cả URLs của mỗi loại media
                                updatePhotoUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                                updateVideoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                                
                                updatePhotoUrl = updatePhotoUrls.FirstOrDefault();
                                updateVideoUrl = updateVideoUrls.FirstOrDefault();
                            }
                        }
                    }
                    catch (Exception mediaEx)
                    {
                        // 🔧 FIX: Không fail toàn bộ request nếu media upload lỗi
                        // Chỉ log lỗi và tiếp tục
                        Console.WriteLine($"WARNING: Media upload failed but progress was created successfully: {mediaEx.Message}");
                        // Không return error, tiếp tục với response không có media
                    }
                }

                // Lấy parameters của progress vừa tạo
                var latestProgressResult = await _processingBatchProgressService.GetAllByBatchIdAsync(batchId, userId, isAdmin, isManager);
                var responseParameters = new List<ProcessingParameterViewAllDto>();
                var actualProgressId = Guid.Empty;
                
                if (latestProgressResult.Status == Const.SUCCESS_READ_CODE && latestProgressResult.Data is List<ProcessingBatchProgressViewAllDto> progressesList)
                {
                    var latestProgressDto = progressesList.LastOrDefault();
                    if (latestProgressDto != null)
                    {
                        actualProgressId = latestProgressDto.ProgressId;
                        responseParameters = latestProgressDto.Parameters ?? new List<ProcessingParameterViewAllDto>();
                    }
                }

                return Ok(new ProcessingBatchProgressMediaResponse
                {
                    Message = result.Message,
                    ProgressId = actualProgressId,
                    PhotoUrl = updatePhotoUrl,
                    VideoUrl = updateVideoUrl,
                    MediaCount = allMediaFiles.Count,
                    AllPhotoUrls = updatePhotoUrls,
                    AllVideoUrls = updateVideoUrls,
                    Parameters = responseParameters
                });
            }
            catch (Exception ex)
            {
                // Trả lỗi rõ ràng về FE
                return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Cập nhật progress cho các giai đoạn tiếp theo (không bị fail)
        /// </summary>
        [HttpPost("update-next-stages/{batchId}")]
        [Authorize(Roles = "Farmer,Admin,BusinessManager")]
        public async Task<IActionResult> UpdateNextStages(Guid batchId, [FromBody] ProcessingBatchProgressCreateRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                Console.WriteLine($"DEBUG CONTROLLER UPDATE NEXT STAGES: batchId: {batchId}, userId: {userId}");

                // Validate input
                if (request == null)
                {
                    return BadRequest(new { message = "Dữ liệu đầu vào không hợp lệ" });
                }

                // Xử lý parameters sử dụng helper
                var parameters = ProcessingHelper.ProcessParameters(
                    request.ParameterName,
                    request.ParameterValue,
                    request.Unit,
                    request.RecordedAt,
                    request.ParametersJson
                );
                
                // Validate parameters
                if (!ProcessingHelper.ValidateParameters(parameters))
                {
                    return BadRequest(new { message = "Dữ liệu parameters không hợp lệ." });
                }

                var dto = new ProcessingBatchProgressCreateDto
                {
                    StageId = request.StageId, // 🔧 MỚI: Truyền StageId từ frontend
                    ProgressDate = request.ProgressDate,
                    OutputQuantity = request.OutputQuantity,
                    OutputUnit = request.OutputUnit,
                    PhotoUrl = null,
                    VideoUrl = null,
                    Parameters = parameters.Any() ? parameters : null
                };

                var result = await _processingBatchProgressService
                    .UpdateNextStagesAsync(batchId, dto, userId, isAdmin, isManager);

                if (result.Status != Const.SUCCESS_CREATE_CODE)
                {
                    if (result.Status == Const.FAIL_CREATE_CODE || result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                        return BadRequest(new { message = result.Message });
                    return StatusCode(500, new { message = result.Message });
                }

                return Ok(new { message = result.Message, progressId = result.Data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}
