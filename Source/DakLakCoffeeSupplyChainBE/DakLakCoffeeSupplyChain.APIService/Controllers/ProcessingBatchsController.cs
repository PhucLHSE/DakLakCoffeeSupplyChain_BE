    using DakLakCoffeeSupplyChain.Common;
    using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs;
    using DakLakCoffeeSupplyChain.Repositories.Models;
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
        public class ProcessingBatchController : ControllerBase
        {
            private readonly IProcessingBatchService _processingbatchservice;

            public ProcessingBatchController(IProcessingBatchService processingbatchservice)
            {
                _processingbatchservice = processingbatchservice;
            }

            // GET: api/processing-batch
            [HttpGet]
            [EnableQuery]
            [Authorize(Roles = "Farmer,Admin, BusinessManager, AgriculturalExpert")]
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
                var isExpert = User.IsInRole("AgriculturalExpert");

                var result = await _processingbatchservice
                .GetAllByUserId(userId, isAdmin, isManager, isExpert);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }

            [HttpPost]
            [Authorize(Roles = "Farmer,Admin, BusinessManager, AgriculturalExpert")]
            public async Task<IActionResult> Create(
                [FromBody] ProcessingBatchCreateDto dto)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                {
                    return BadRequest("Không thể lấy userId từ token.");
                }

                var result = await _processingbatchservice
                   .CreateAsync(dto, userId);

                if (result.Status == Const.SUCCESS_CREATE_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return Conflict(result.Message);

                return BadRequest(result.Message);
            }

            [HttpPut]
            [Authorize(Roles = "Farmer,Admin, BusinessManager, AgriculturalExpert")]
            public async Task<IActionResult> Update(
                [FromBody] ProcessingBatchUpdateDto dto)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                var result = await _processingbatchservice
                   .UpdateAsync(dto, userId, isAdmin, isManager);

                if (result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return BadRequest(result.Message);
            }

            [HttpPatch("{id}/soft-delete")]
            [Authorize(Roles = "Farmer,Admin, BusinessManager, AgriculturalExpert")]
            public async Task<IActionResult> SoftDelete(Guid id)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");

                var result = await _processingbatchservice
                   .SoftDeleteAsync(id, userId, isAdmin, isManager);

                if (result.Status == Const.SUCCESS_DELETE_CODE)
                    return Ok(result.Message);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return BadRequest(result.Message);
            }

            [HttpDelete("hard/{id}")]
            [Authorize(Roles = "Farmer,Admin, BusinessManager, AgriculturalExpert")]
            public async Task<IActionResult> HardDelete(Guid id)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");

                var isManager = User.IsInRole("BusinessManager");

                var result = await _processingbatchservice
                   .HardDeleteAsync(id, userId, isManager, isAdmin);

                if (result.Status == Const.SUCCESS_DELETE_CODE)
                    return Ok(result.Message);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return BadRequest(result.Message);
            }
      
            [HttpGet("available-coffee-types")]
            [Authorize(Roles = "Farmer,Admin, BusinessManager, AgriculturalExpert")]
            public async Task<IActionResult> GetAvailableCoffeeTypes(
                [FromQuery] Guid cropSeasonId)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var result = await _processingbatchservice
                   .GetAvailableCoffeeTypesAsync(userId, cropSeasonId);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return BadRequest(result.Message);
            }

            [HttpGet("{id}/full-details")]
            [Authorize(Roles = "Farmer,Admin,BusinessManager, AgriculturalExpert")]
            public async Task<IActionResult> GetFullDetails(Guid id)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("BusinessManager");
                var isExpert = User.IsInRole("AgriculturalExpert");

                var result = await _processingbatchservice
                   .GetFullDetailsAsync(id, userId, isAdmin, isManager, isExpert);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }

            // GET: api/ProcessingBatch/business-manager/farmer/{farmerId}
            [HttpGet("business-manager/farmer/{farmerId}")]
            [Authorize(Roles = "BusinessManager")]
            public async Task<IActionResult> GetBatchesByFarmerForBusinessManager(Guid farmerId)
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var result = await _processingbatchservice
                   .GetBatchesByFarmerForBusinessManagerAsync(userId, farmerId);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }

            // GET: api/ProcessingBatch/business-manager/farmers
            [HttpGet("business-manager/farmers")]
            [Authorize(Roles = "BusinessManager")]
            public async Task<IActionResult> GetFarmersWithBatchesForBusinessManager()
            {
                var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdStr, out var userId))
                    return BadRequest("Không thể lấy userId từ token.");

                var result = await _processingbatchservice
                   .GetFarmersWithBatchesForBusinessManagerAsync(userId);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }
        }
    }
