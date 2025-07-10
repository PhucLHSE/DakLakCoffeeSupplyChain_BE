using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingWasteController : ControllerBase
    {
        private readonly IProcessingWasteService _processingWasteService;

        public ProcessingWasteController(IProcessingWasteService processingWasteService)
        {
            _processingWasteService = processingWasteService;
        }

        // GET: api/processingwaste
        [HttpGet]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetAll()
        {
            // Lấy userId từ token
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            // Gọi Service
            var result = await _processingWasteService.GetAllByUserIdAsync(userId, isAdmin);

            // Trả kết quả
            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            var result = await _processingWasteService.GetByIdAsync(id, userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(403, result.Message);
        }
        [HttpPost]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> Create([FromBody] ProcessingWasteCreateDto dto)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            var result = await _processingWasteService.CreateAsync(dto, userId, isAdmin);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }
        // PUT: api/processingwaste/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProcessingWasteUpdateDto dto)
        {
            // Lấy userId từ token
            var userIdStr = User.FindFirst("userId")?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            // Gọi service để cập nhật chất thải
            var result = await _processingWasteService.UpdateAsync(id, dto, userId, isAdmin);

            // Trả về kết quả dựa trên trạng thái của service
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);  // Thành công, trả về dữ liệu đã cập nhật

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);  // Không tìm thấy dữ liệu cần cập nhật

            return BadRequest(result.Message);  // Lỗi khi cập nhật (ví dụ: dữ liệu không hợp lệ)
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin"); 

            var result = await _processingWasteService.SoftDeleteAsync(id, userId, isAdmin);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }

        [HttpDelete("hard/{id}")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể lấy userId từ token.");

            var isAdmin = User.IsInRole("Admin"); 

            var result = await _processingWasteService.HardDeleteAsync(id, userId, isAdmin);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }
    }
}
