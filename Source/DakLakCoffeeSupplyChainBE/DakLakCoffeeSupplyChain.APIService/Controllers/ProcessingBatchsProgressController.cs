using DakLakCoffeeSupplyChain.APIService.Requests.ProcessingBatchProgressReques;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
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

        public ProcessingBatchsProgressController(
            IProcessingBatchProgressService processingBatchProgressService, 
            IUploadService uploadService, 
            IMediaService mediaService,
            IUnitOfWork unitOfWork)
        {
            _processingBatchProgressService = processingBatchProgressService;
            _uploadService = uploadService;
            _mediaService = mediaService;
            _unitOfWork = unitOfWork;
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

            // Lấy thông tin batch
            var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                b => b.BatchId == batchId && !b.IsDeleted,
                include: q => q
                    .Include(b => b.Method)
                    .Include(b => b.CropSeason)
                    .Include(b => b.CoffeeType)
                    .Include(b => b.Farmer).ThenInclude(f => f.User)
                    .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted))
            );

            if (batch == null)
                return NotFound("Không tìm thấy lô chế biến.");

            // Kiểm tra quyền truy cập
            if (!isAdmin && !isManager)
            {
                var farmer = await _unitOfWork.FarmerRepository
                    .GetByIdAsync(f => f.UserId == userId && !f.IsDeleted);

                if (farmer == null)
                    return BadRequest("Không tìm thấy thông tin nông hộ.");

                if (batch.FarmerId != farmer.FarmerId)
                    return Forbid("Bạn không có quyền truy cập lô chế biến này.");
            }

            // Tính toán khối lượng còn lại
            var totalProcessedQuantity = batch.ProcessingBatchProgresses
                .Where(p => p.OutputQuantity.HasValue)
                .Sum(p => p.OutputQuantity.Value);

            var remainingQuantity = batch.InputQuantity - totalProcessedQuantity;
            var canCreateProgress = remainingQuantity > 0;

            return Ok(new
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                Status = batch.Status,
                CanCreateProgress = canCreateProgress,
                TotalInputQuantity = batch.InputQuantity,
                TotalProcessedQuantity = totalProcessedQuantity,
                RemainingQuantity = remainingQuantity,
                InputUnit = batch.InputUnit,
                Message = canCreateProgress 
                    ? $"Có thể tạo tiến độ. Còn lại {remainingQuantity} {batch.InputUnit}" 
                    : $"Không thể tạo tiến độ. Đã chế biến hết khối lượng."
            });
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
            try
            {
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");
                var isFarmer = User.IsInRole("Farmer");

                // Lấy thông tin batch và stages
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(batchId);
                if (batch == null)
                    return BadRequest(new { message = "Batch không tồn tại." });

                var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                    s => s.MethodId == batch.MethodId && !s.IsDeleted,
                    q => q.OrderBy(s => s.OrderIndex)
                );

                var progresses = await _unitOfWork.ProcessingBatchProgressRepository.GetAllAsync(
                    p => p.BatchId == batchId && !p.IsDeleted,
                    q => q.OrderByDescending(p => p.StepIndex)
                );

                var latestProgress = progresses.FirstOrDefault();

                return Ok(new
                {
                    message = "Debug advance info",
                    batchId,
                    userId,
                    batchStatus = batch.Status,
                    roles = new
                    {
                        isAdmin,
                        isManager,
                        isFarmer
                    },
                    stages = stages.Select(s => new
                    {
                        stageId = s.StageId,
                        stageName = s.StageName,
                        orderIndex = s.OrderIndex
                    }).ToList(),
                    totalStages = stages.Count(),
                    progresses = progresses.Select(p => new
                    {
                        progressId = p.ProgressId,
                        stepIndex = p.StepIndex,
                        stageId = p.StageId,
                        progressDate = p.ProgressDate
                    }).ToList(),
                    totalProgresses = progresses.Count(),
                    latestProgress = latestProgress != null ? new
                    {
                        progressId = latestProgress.ProgressId,
                        stepIndex = latestProgress.StepIndex,
                        stageId = latestProgress.StageId
                    } : null,
                    note = "Chỉ Farmer mới được phép advance progress"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi debug: {ex.Message}" });
            }
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
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");

                var isManager = User.IsInRole("BusinessManager");

                // Tạo parameters - chỉ sử dụng single parameter
                var parameters = new List<ProcessingParameterInProgressDto>();
                
                Console.WriteLine($"DEBUG CONTROLLER CREATE: Single parameter: {request.ParameterName} = {request.ParameterValue} {request.Unit}");
                
                if (!string.IsNullOrEmpty(request.ParameterName))
                {
                    Console.WriteLine($"DEBUG CONTROLLER CREATE: Adding single parameter: {request.ParameterName} = {request.ParameterValue} {request.Unit}");
                    parameters.Add(new ProcessingParameterInProgressDto
                    {
                        ParameterName = request.ParameterName,
                        ParameterValue = request.ParameterValue,
                        Unit = request.Unit,
                        RecordedAt = request.RecordedAt
                    });
                }
                else
                {
                    Console.WriteLine("DEBUG CONTROLLER CREATE: No parameter found in request");
                }

                // Tạo progress trước, sau đó upload media
                var dto = new ProcessingBatchProgressCreateDto
                {
                    StageId = request.StageId, // Thêm StageId từ request
                    ProgressDate = request.ProgressDate,
                    OutputQuantity = request.OutputQuantity,
                    OutputUnit = request.OutputUnit,
                    PhotoUrl = null, // Sẽ được cập nhật sau
                    VideoUrl = null, // Sẽ được cập nhật sau
                    Parameters = parameters.Any() ? parameters : null
                };

                var result = await _processingBatchProgressService
                    .CreateAsync(batchId, dto, userId, isAdmin, isManager);

                if (result.Status != Const.SUCCESS_CREATE_CODE)
                    return BadRequest(new { message = result.Message });

                if (result.Data is not Guid progressId)
                    return StatusCode(500, new { message = "Không lấy được progressId sau khi tạo." });

                string? photoUrl = null, videoUrl = null;
                List<string> photoUrls = new List<string>();
                List<string> videoUrls = new List<string>();

                // Upload media sau khi có progressId để tránh conflict
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
                            relatedId: progressId, // Sử dụng progressId thực tế
                            uploadedBy: userId.ToString()
                        );

                        // Lấy tất cả URLs của mỗi loại media
                        photoUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                        videoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                        
                        photoUrl = photoUrls.FirstOrDefault();
                        videoUrl = videoUrls.FirstOrDefault();

                                                 // Không cần cập nhật PhotoUrl và VideoUrl trong database nữa
                         // Hệ thống sẽ tự động lấy từ MediaFile table dựa trên ProgressId
                    }
                    catch (Exception mediaEx)
                    {
                        // Log lỗi media nhưng không fail toàn bộ request
                        // Progress đã được tạo thành công, chỉ có media bị lỗi
                        return StatusCode(500, new { message = $"Progress đã tạo thành công nhưng lỗi upload media: {mediaEx.Message}" });
                    }
                }

                // Lấy parameters của progress vừa tạo
                var progressWithParams = await _processingBatchProgressService.GetByIdAsync(progressId);
                var responseParameters = new List<ProcessingParameterViewAllDto>();
                
                if (progressWithParams.Status == Const.SUCCESS_READ_CODE && progressWithParams.Data is ProcessingBatchProgressDetailDto detailDto)
                {
                    responseParameters = detailDto.Parameters ?? new List<ProcessingParameterViewAllDto>();
                }

                return StatusCode(StatusCodes.Status201Created, new ProcessingBatchProgressMediaResponse
                {
                    Message = result.Message,
                    ProgressId = progressId,
                    PhotoUrl = photoUrl,
                    VideoUrl = videoUrl,
                    MediaCount = allMediaFiles.Count,
                    AllPhotoUrls = photoUrls ?? new List<string>(),
                    AllVideoUrls = videoUrls ?? new List<string>(),
                    Parameters = responseParameters
                });
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
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                string? photoUrl = null;
                string? videoUrl = null;
                List<string> photoUrls = new List<string>();
                List<string> videoUrls = new List<string>();

                // Tạo parameters - chỉ sử dụng single parameter
                var parameters = new List<ProcessingParameterInProgressDto>();
                
                Console.WriteLine($"DEBUG CONTROLLER ADVANCE: Single parameter: {request.ParameterName} = {request.ParameterValue} {request.Unit}");
                
                if (!string.IsNullOrEmpty(request.ParameterName))
                {
                    Console.WriteLine($"DEBUG CONTROLLER ADVANCE: Adding single parameter: {request.ParameterName} = {request.ParameterValue} {request.Unit}");
                    parameters.Add(new ProcessingParameterInProgressDto
                    {
                        ParameterName = request.ParameterName,
                        ParameterValue = request.ParameterValue,
                        Unit = request.Unit,
                        RecordedAt = request.RecordedAt
                    });
                }
                else
                {
                    Console.WriteLine("DEBUG CONTROLLER ADVANCE: No parameter found in request");
                }

                // Tạo progress trước
                var dto = new AdvanceProcessingBatchProgressDto
                {
                    ProgressDate = request.ProgressDate,
                    OutputQuantity = request.OutputQuantity,
                    OutputUnit = request.OutputUnit,
                    PhotoUrl = null, // Sẽ được cập nhật sau
                    VideoUrl = null, // Sẽ được cập nhật sau
                    Parameters = parameters.Any() ? parameters : null,
                    StageId = request.StageId, // Thêm stageId từ request
                    CurrentStageId = request.CurrentStageId, // Thêm currentStageId từ request
                    StageDescription = request.StageDescription // Thêm stageDescription từ request
                };

                var result = await _processingBatchProgressService
                    .AdvanceProgressByBatchIdAsync(batchId, dto, userId, isAdmin, isManager);

                if (result.Status != Const.SUCCESS_CREATE_CODE && result.Status != Const.SUCCESS_UPDATE_CODE)
                {
                    if (result.Status == Const.FAIL_CREATE_CODE || result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                        return BadRequest(new { message = result.Message });
                    return StatusCode(500, new { message = result.Message });
                }

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
                        // Hoặc có thể cần thêm logic để lấy progressId mới nhất của batch
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
                                photoUrls = mediaList.Where(m => m.MediaType == "image").Select(m => m.MediaUrl).ToList();
                                videoUrls = mediaList.Where(m => m.MediaType == "video").Select(m => m.MediaUrl).ToList();
                                
                                photoUrl = photoUrls.FirstOrDefault();
                                videoUrl = videoUrls.FirstOrDefault();
                            }
                        }
                    }
                    catch (Exception mediaEx)
                    {
                        // Progress đã tạo thành công, chỉ có media bị lỗi
                        return StatusCode(500, new { message = $"Progress đã tạo thành công nhưng lỗi upload media: {mediaEx.Message}" });
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
                    PhotoUrl = photoUrl,
                    VideoUrl = videoUrl,
                    MediaCount = allMediaFiles.Count,
                    AllPhotoUrls = photoUrls,
                    AllVideoUrls = videoUrls,
                    Parameters = responseParameters
                });
            }
            catch (Exception ex)
            {
                // Trả lỗi rõ ràng về FE
                return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}
