using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
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

            // 🔧 FIX: Lấy OutputQuantity của bước cuối cùng (StepIndex cao nhất)
            // Vì bước cuối mới là sản lượng thực tế cuối cùng
            var finalProgress = batch.ProcessingBatchProgresses
                .Where(p => p.OutputQuantity.HasValue && p.OutputQuantity.Value > 0)
                .OrderByDescending(p => p.StepIndex)  // Tìm StepIndex cao nhất
                .FirstOrDefault();
            var finalOutputQuantity = finalProgress?.OutputQuantity ?? 0;

            var remainingQuantity = batch.InputQuantity - finalOutputQuantity;
            var canCreateProgress = remainingQuantity > 0;

                         return Ok(new
             {
                 BatchId = batch.BatchId,
                 BatchCode = batch.BatchCode,
                 Status = batch.Status,
                 CanCreateProgress = canCreateProgress,
                 TotalInputQuantity = batch.InputQuantity,
                 TotalProcessedQuantity = finalOutputQuantity,
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
            try
            {
                var userIdStr = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest(new { message = "Không thể lấy userId từ token." });

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                // Lấy thông tin batch và progress cuối cùng
                var batch = await _unitOfWork.ProcessingBatchRepository.GetByIdAsync(
                    b => b.BatchId == batchId && !b.IsDeleted,
                    include: q => q
                        .Include(b => b.ProcessingBatchProgresses.Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedAt))
                        .Include(b => b.Method)
                        .ThenInclude(m => m.ProcessingStages.Where(s => !s.IsDeleted).OrderBy(s => s.OrderIndex))
                );

                Console.WriteLine($"DEBUG RETRY INFO: Batch found: {batch?.BatchId}");
                Console.WriteLine($"DEBUG RETRY INFO: Method: {batch?.Method?.MethodCode}");
                Console.WriteLine($"DEBUG RETRY INFO: Progresses count: {batch?.ProcessingBatchProgresses?.Count() ?? 0}");

                if (batch == null)
                    return NotFound(new { message = "Không tìm thấy lô chế biến." });

                // Kiểm tra quyền truy cập
                if (!isAdmin && !isManager)
                {
                    var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                        f => f.UserId == userId && !f.IsDeleted
                    );
                    if (farmer == null || batch.FarmerId != farmer.FarmerId)
                        return Forbid();
                }

                // Lấy progress cuối cùng (không bị xóa)
                var lastProgress = batch.ProcessingBatchProgresses.FirstOrDefault();
                if (lastProgress == null)
                    return NotFound(new { message = "Không tìm thấy tiến trình nào cho lô này." });

                Console.WriteLine($"DEBUG RETRY INFO: LastProgress StageId = {lastProgress.StageId}");
                Console.WriteLine($"DEBUG RETRY INFO: Available stages count = {batch.Method.ProcessingStages.Count()}");
                foreach (var stage in batch.Method.ProcessingStages)
                {
                    Console.WriteLine($"DEBUG RETRY INFO: Stage {stage.StageId} - {stage.StageName}");
                }

                // Lấy stage hiện tại
                var currentStage = batch.Method.ProcessingStages.FirstOrDefault(s => s.StageId == lastProgress.StageId);
                if (currentStage == null)
                {
                    // 🔧 FIX: Fallback - sử dụng stage đầu tiên nếu không tìm thấy
                    currentStage = batch.Method.ProcessingStages.FirstOrDefault();
                    if (currentStage == null)
                    {
                        // 🔧 FIX: Thử lấy stage từ database trực tiếp
                        var stages = await _unitOfWork.ProcessingStageRepository.GetAllAsync(
                            s => s.MethodId == batch.MethodId && !s.IsDeleted,
                            q => q.OrderBy(s => s.OrderIndex)
                        );
                        currentStage = stages.FirstOrDefault();
                        
                        if (currentStage == null)
                            return NotFound(new { message = "Không tìm thấy thông tin giai đoạn nào." });
                        
                        Console.WriteLine($"DEBUG RETRY INFO: Using direct query stage: {currentStage.StageId} - {currentStage.StageName}");
                    }
                    else
                    {
                        Console.WriteLine($"DEBUG RETRY INFO: Using fallback stage: {currentStage.StageId} - {currentStage.StageName}");
                    }
                }

                // Tính toán thông tin retry
                var finalOutputBeforeRetry = lastProgress.OutputQuantity ?? 0;
                var finalOutputUnit = lastProgress.OutputUnit ?? "kg";
                var maxWastePercentage = GetMaxWastePercentageForStage(currentStage.StageName);

                var retryInfo = new
                {
                    finalOutputBeforeRetry = finalOutputBeforeRetry,
                    finalOutputUnit = finalOutputUnit,
                    maxAllowedRetryQuantity = finalOutputBeforeRetry, // Không được vượt quá output cuối cùng
                    calculatedWaste = 0, // Sẽ được tính khi user nhập
                    wastePercentage = 0, // Sẽ được tính khi user nhập
                    maxWastePercentage = maxWastePercentage,
                    isValid = true,
                    errorMessage = (string?)null
                };

                return Ok(retryInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
            }
        }

        // 🔧 MỚI: Helper method để lấy tỷ lệ waste tối đa cho từng stage
        private double GetMaxWastePercentageForStage(string stageName)
        {
            return stageName?.ToLower() switch
            {
                "thu hoạch" => 20.0,
                "phơi" => 15.0,
                "xay vỏ" => 10.0,
                "phân loại" => 8.0,
                "đóng gói" => 5.0,
                _ => 15.0 // Default
            };
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

                // Tạo parameters - hỗ trợ nhiều parameter
                var parameters = new List<ProcessingParameterInProgressDto>();
                
                Console.WriteLine($"DEBUG CONTROLLER UPDATE: Single parameter: {request.ParameterName} = {request.ParameterValue} {request.Unit}");
                
                // Single parameter
                if (!string.IsNullOrEmpty(request.ParameterName))
                {
                    Console.WriteLine($"DEBUG CONTROLLER UPDATE: Adding single parameter: {request.ParameterName} = {request.ParameterValue} {request.Unit}");
                    parameters.Add(new ProcessingParameterInProgressDto
                    {
                        ParameterName = request.ParameterName,
                        ParameterValue = request.ParameterValue,
                        Unit = request.Unit,
                        RecordedAt = request.RecordedAt
                    });
                }
                
                // Multiple parameters từ JSON array
                if (!string.IsNullOrEmpty(request.ParametersJson))
                {
                    try
                    {
                        var multipleParams = System.Text.Json.JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(request.ParametersJson);
                        if (multipleParams != null)
                        {
                            Console.WriteLine($"DEBUG CONTROLLER UPDATE: Adding {multipleParams.Count} parameters from JSON");
                            parameters.AddRange(multipleParams);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing parameters JSON: {ex.Message}");
                    }
                }
                
                if (parameters.Count == 0)
                {
                    Console.WriteLine("DEBUG CONTROLLER UPDATE: No parameter found in request");
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

                // Tạo parameters - hỗ trợ nhiều parameter
                var parameters = new List<ProcessingParameterInProgressDto>();
                
                // Single parameter
                if (!string.IsNullOrEmpty(request.ParameterName))
                {
                    parameters.Add(new ProcessingParameterInProgressDto
                    {
                        ParameterName = request.ParameterName,
                        ParameterValue = request.ParameterValue,
                        Unit = request.Unit,
                        RecordedAt = request.RecordedAt
                    });
                }
                
                // Multiple parameters từ JSON array
                if (!string.IsNullOrEmpty(request.ParametersJson))
                {
                    try
                    {
                        var multipleParams = System.Text.Json.JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(request.ParametersJson);
                        if (multipleParams != null)
                        {
                            parameters.AddRange(multipleParams);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing parameters JSON: {ex.Message}");
                    }
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
