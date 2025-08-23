using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        #region Evaluation Criteria APIs

        /// <summary>
        /// Lấy tiêu chí đánh giá cho stage cụ thể (hỗ trợ cả stageCode và stageId)
        /// </summary>
        /// <param name="stageCode">Mã stage hoặc ID stage</param>
        /// <returns>Danh sách tiêu chí đánh giá</returns>
        [HttpGet("criteria/{stageCode}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,Farmer")]
        public IActionResult GetCriteriaForStage(string stageCode)
        {
            try
            {
                Console.WriteLine($"🔍 DEBUG: GetCriteriaForStage called with: '{stageCode}'");
                
                string actualStageCode = stageCode;
                
                // 🔧 CẢI THIỆN: Kiểm tra nếu stageCode là số (stageId) thì map sang stageCode
                if (int.TryParse(stageCode, out int stageId))
                {
                    Console.WriteLine($"🔍 DEBUG: Detected stageId: {stageId}, mapping to stageCode...");
                    actualStageCode = MapStageIdToStageCode(stageId);
                    Console.WriteLine($"🔍 DEBUG: Mapped stageId {stageId} to stageCode: '{actualStageCode}'");
                    
                    if (string.IsNullOrEmpty(actualStageCode))
                    {
                        Console.WriteLine($"❌ ERROR: Could not map stageId {stageId} to stageCode");
                        return NotFound(new
                        {
                            status = Const.WARNING_NO_DATA_CODE,
                            message = $"Không thể map stageId {stageId} sang stageCode",
                            data = new List<object>()
                        });
                    }
                }
                
                Console.WriteLine($"🔍 DEBUG: Getting criteria for stageCode: '{actualStageCode}'");
                var stageInfo = StageFailureParser.GetStageFailureInfo(actualStageCode);
                var criteria = stageInfo.GetType().GetProperty("criteria")?.GetValue(stageInfo) as List<object> ?? new List<object>();
                
                Console.WriteLine($"🔍 DEBUG: Found {criteria.Count} criteria for stageCode '{actualStageCode}'");
                
                if (!criteria.Any())
                {
                    Console.WriteLine($"❌ WARNING: No criteria found for stageCode: '{actualStageCode}'");
                    return NotFound(new
                    {
                        status = Const.WARNING_NO_DATA_CODE,
                        message = $"Không tìm thấy tiêu chí đánh giá cho stage: {actualStageCode}",
                        data = new List<object>()
                    });
                }

                Console.WriteLine($"✅ SUCCESS: Returning {criteria.Count} criteria for stageCode '{actualStageCode}'");
                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy tiêu chí đánh giá thành công",
                    data = criteria
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION: Error in GetCriteriaForStage for '{stageCode}': {ex.Message}");
                Console.WriteLine($"❌ EXCEPTION: Stack trace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy tiêu chí đánh giá: {ex.Message}",
                    data = new List<object>()
                });
            }
        }

        /// <summary>
        /// Lấy tiêu chí đánh giá cho stage cụ thể (sử dụng stageId int)
        /// </summary>
        /// <param name="stageId">ID của stage từ database</param>
        /// <returns>Danh sách tiêu chí đánh giá</returns>
        [HttpGet("criteria-by-id/{stageId:int}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,Farmer")]
        public IActionResult GetCriteriaForStageById(int stageId)
        {
            try
            {
                Console.WriteLine($"🔍 DEBUG: GetCriteriaForStageById called with stageId: {stageId}");
                
                // 🔧 CẢI THIỆN: Map stageId sang stageCode tương ứng
                var stageCode = MapStageIdToStageCode(stageId);
                Console.WriteLine($"🔍 DEBUG: Mapped stageId {stageId} to stageCode: '{stageCode}'");
                
                if (string.IsNullOrEmpty(stageCode))
                {
                    Console.WriteLine($"❌ ERROR: Could not map stageId {stageId} to stageCode");
                    return NotFound(new
                    {
                        status = Const.WARNING_NO_DATA_CODE,
                        message = $"Không thể map stageId {stageId} sang stageCode",
                        data = new List<object>()
                    });
                }

                Console.WriteLine($"🔍 DEBUG: Getting criteria for stageCode: '{stageCode}'");
                var stageInfo = StageFailureParser.GetStageFailureInfo(stageCode);
                var criteria = stageInfo.GetType().GetProperty("criteria")?.GetValue(stageInfo) as List<object> ?? new List<object>();
                
                Console.WriteLine($"🔍 DEBUG: Found {criteria.Count} criteria for stageCode '{stageCode}'");
                
                if (!criteria.Any())
                {
                    Console.WriteLine($"❌ WARNING: No criteria found for stageCode: '{stageCode}'");
                    return NotFound(new
                    {
                        status = Const.WARNING_NO_DATA_CODE,
                        message = $"Không tìm thấy tiêu chí đánh giá cho stage: {stageCode}",
                        data = new List<object>()
                    });
                }

                Console.WriteLine($"✅ SUCCESS: Returning {criteria.Count} criteria for stageCode '{stageCode}'");
                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy tiêu chí đánh giá thành công",
                    data = criteria
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION: Error in GetCriteriaForStageById for stageId {stageId}: {ex.Message}");
                Console.WriteLine($"❌ EXCEPTION: Stack trace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy tiêu chí đánh giá: {ex.Message}",
                    data = new List<object>()
                });
            }
        }

        /// <summary>
        /// Map stageId (int) sang stageCode (string) tương ứng
        /// </summary>
        /// <param name="stageId">ID của stage từ database</param>
        /// <returns>Stage code tương ứng</returns>
        private string MapStageIdToStageCode(int stageId)
        {
            try
            {
                // 🔧 CẢI THIỆN: Lấy stageCode trực tiếp từ database thay vì map từ stageName
                var stage = _unitOfWork.ProcessingStageRepository.GetByIdAsync(stageId).Result;
                
                if (stage != null && !stage.IsDeleted)
                {
                    // Sử dụng stageCode trực tiếp từ database (đã có sẵn: "harvest", "drying", "hulling", etc.)
                    return stage.StageCode;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting stage code for ID {stageId}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Lấy stageName từ stageId bằng cách query database
        /// </summary>
        /// <param name="stageId">ID của stage</param>
        /// <returns>Tên của stage</returns>
        private string GetStageNameById(int stageId)
        {
            try
            {
                // 🔧 CẢI THIỆN: Query database để lấy stageName từ stageId
                // Sử dụng UnitOfWork để truy cập ProcessingStageRepository
                var stage = _unitOfWork.ProcessingStageRepository.GetByIdAsync(stageId).Result;
                
                if (stage != null && !stage.IsDeleted)
                {
                    return stage.StageName;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                // Log error và return empty string
                Console.WriteLine($"Error getting stage name for ID {stageId}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Lấy thông tin stage và tiêu chí để hiển thị khi fail
        /// </summary>
        /// <param name="stageCode">Mã stage</param>
        /// <returns>Thông tin stage, tiêu chí và lý do không đạt</returns>
        [HttpGet("stage-failure-info/{stageCode}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,Farmer")]
        public IActionResult GetStageFailureInfo(string stageCode)
        {
            try
            {
                var stageInfo = StageFailureParser.GetStageFailureInfo(stageCode);
                
                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy thông tin stage failure thành công",
                    data = stageInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy thông tin stage failure: {ex.Message}",
                    data = new object()
                });
            }
        }

        /// <summary>
        /// Lấy lý do không đạt cho stage cụ thể (hỗ trợ cả stageCode và stageId)
        /// </summary>
        /// <param name="stageCode">Mã stage hoặc ID stage</param>
        /// <returns>Danh sách lý do không đạt</returns>
        [HttpGet("failure-reasons/{stageCode}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,Farmer")]
        public IActionResult GetFailureReasonsForStage(string stageCode)
        {
            try
            {
                Console.WriteLine($"🔍 DEBUG: GetFailureReasonsForStage called with: '{stageCode}'");
                
                string actualStageCode = stageCode;
                
                // 🔧 CẢI THIỆN: Kiểm tra nếu stageCode là số (stageId) thì map sang stageCode
                if (int.TryParse(stageCode, out int stageId))
                {
                    Console.WriteLine($"🔍 DEBUG: Detected stageId: {stageId}, mapping to stageCode...");
                    actualStageCode = MapStageIdToStageCode(stageId);
                    Console.WriteLine($"🔍 DEBUG: Mapped stageId {stageId} to stageCode: '{actualStageCode}'");
                    
                    if (string.IsNullOrEmpty(actualStageCode))
                    {
                        Console.WriteLine($"❌ ERROR: Could not map stageId {stageId} to stageCode");
                        return NotFound(new
                        {
                            status = Const.WARNING_NO_DATA_CODE,
                            message = $"Không thể map stageId {stageId} sang stageCode",
                            data = new List<object>()
                        });
                    }
                }
                
                Console.WriteLine($"🔍 DEBUG: Getting failure reasons for stageCode: '{actualStageCode}'");
                var stageInfo = StageFailureParser.GetStageFailureInfo(actualStageCode);
                var reasons = stageInfo.GetType().GetProperty("failureReasons")?.GetValue(stageInfo) as List<object> ?? new List<object>();
                
                Console.WriteLine($"🔍 DEBUG: Found {reasons.Count} failure reasons for stageCode '{actualStageCode}'");
                
                if (!reasons.Any())
                {
                    Console.WriteLine($"❌ WARNING: No failure reasons found for stageCode: '{actualStageCode}'");
                    return NotFound(new
                    {
                        status = Const.WARNING_NO_DATA_CODE,
                        message = $"Không tìm thấy lý do không đạt cho stage: {actualStageCode}",
                        data = new List<object>()
                    });
                }

                Console.WriteLine($"✅ SUCCESS: Returning {reasons.Count} failure reasons for stageCode '{actualStageCode}'");
                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy lý do không đạt thành công",
                    data = reasons
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION: Error in GetFailureReasonsForStage for '{stageCode}': {ex.Message}");
                Console.WriteLine($"❌ EXCEPTION: Stack trace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy lý do không đạt: {ex.Message}",
                    data = new List<object>()
                });
            }
        }

        /// <summary>
        /// Lấy tất cả tiêu chí đánh giá cho tất cả stages
        /// </summary>
        /// <returns>Dictionary với key là stageCode, value là danh sách tiêu chí</returns>
        [HttpGet("all-criteria")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert")]
        public IActionResult GetAllCriteria()
        {
            try
            {
                var stageCodes = new[] { "harvest", "drying", "hulling", "grading", "fermentation", "washing", "pulping" };
                var allCriteria = new Dictionary<string, object>();

                foreach (var stageCode in stageCodes)
                {
                    allCriteria[stageCode] = StageFailureParser.GetStageFailureInfo(stageCode);
                }

                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy tất cả tiêu chí đánh giá thành công",
                    data = allCriteria
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy tất cả tiêu chí đánh giá: {ex.Message}",
                    data = new Dictionary<string, object>()
                });
            }
        }

        /// <summary>
        /// Lấy tất cả lý do không đạt cho tất cả stages
        /// </summary>
        /// <returns>Dictionary với key là stageCode, value là danh sách lý do</returns>
        [HttpGet("all-failure-reasons")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert")]
        public IActionResult GetAllFailureReasons()
        {
            try
            {
                var stageCodes = new[] { "harvest", "drying", "hulling", "grading", "fermentation", "washing", "pulping" };
                var allReasons = new Dictionary<string, object>();

                foreach (var stageCode in stageCodes)
                {
                    var stageInfo = StageFailureParser.GetStageFailureInfo(stageCode);
                    allReasons[stageCode] = stageInfo.GetType().GetProperty("failureReasons")?.GetValue(stageInfo) as List<object> ?? new List<object>();
                }

                return Ok(new
                {
                    status = Const.SUCCESS_READ_CODE,
                    message = "Lấy tất cả lý do không đạt thành công",
                    data = allReasons
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = Const.ERROR_EXCEPTION,
                    message = $"Lỗi khi lấy tất cả lý do không đạt: {ex.Message}",
                    data = new Dictionary<string, object>()
                });
            }
        }

        #endregion
    }
}

