using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWasteDisposalDTOs;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingWasteDisposalController : ControllerBase
    {
        private readonly IProcessingWasteDisposalService _disposalService;

        public ProcessingWasteDisposalController(IProcessingWasteDisposalService disposalService)
        {
            _disposalService = disposalService;
        }

        // Lấy toàn bộ theo user
        [HttpGet("view-all")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể xác định userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            var result = await _disposalService.GetAllAsync(userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // Lấy chi tiết 1 bản ghi
        [HttpGet("detail/{id}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _disposalService.GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // Tạo mới
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> Create([FromBody] ProcessingWasteDisposalCreateDto input)
        {
            var userIdStr = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var result = await _disposalService.CreateAsync(input, userId); 

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(StatusCodes.Status201Created, result.Message);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            return BadRequest("Có lỗi xảy ra.");
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Farmer, BusinessManager")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProcessingWasteDisposalUpdateDto dto)
        {
            var userIdStr = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var result = await _disposalService.UpdateAsync(id, dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }


        // Xóa mềm

        [HttpPatch("{id}/soft-delete")]
        [Authorize(Roles = "Farmer,BusinessManager")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            // ❌ Không cho Admin
            var isManager = User.IsInRole("BusinessManager");

            var result = await _disposalService.SoftDeleteAsync(id, userId, isManager);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }
        //// Xóa cứng
        [HttpDelete("hard/{id}")]
        [Authorize(Roles = "Farmer,BusinessManager")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            // ❌ Không cho Admin
            var isManager = User.IsInRole("BusinessManager");

            var result = await _disposalService.HardDeleteAsync(id, userId, isManager);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }
    }
}