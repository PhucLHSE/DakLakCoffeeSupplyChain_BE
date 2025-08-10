using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryLogsController : ControllerBase
    {
        private readonly IInventoryLogService _inventoryLogService;

        public InventoryLogsController(IInventoryLogService inventoryLogService)
        {
            _inventoryLogService = inventoryLogService;
        }

        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessStaff,BusinessManager,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out Guid userId))
                return StatusCode(500, "Không xác định được người dùng.");

            var result = await _inventoryLogService
                .GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(403, result.Message);
        }

        [HttpGet("by-inventory/{inventoryId}")]
        [Authorize(Roles = "BusinessStaff,BusinessManager,Admin")]
        public async Task<IActionResult> GetByInventoryId(Guid inventoryId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out Guid userId))
                return StatusCode(500, "Không xác định được người dùng.");

            var result = await _inventoryLogService
                .GetLogsByInventoryIdAsync(inventoryId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(403, result.Message); // Nếu bị chặn quyền, trả 403
        }

        // PATCH: api/InventoryLogs/soft-delete/{logId}
        [HttpPatch("soft-delete/{logId}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> SoftDelete(Guid logId)
        {
            var result = await _inventoryLogService
                .SoftDeleteAsync(logId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy bản ghi tồn kho để xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // DELETE: api/InventoryLogs/hard-delete/{logId}
        [HttpDelete("hard-delete/{logId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid logId)
        {
            var result = await _inventoryLogService
                .HardDeleteAsync(logId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa vĩnh viễn thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy bản ghi tồn kho để xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa vĩnh viễn thất bại.");

            return StatusCode(500, result.Message);
        }

        [HttpGet("{logId}")]
        [Authorize(Roles = "BusinessStaff,BusinessManager,Admin")]
        public async Task<IActionResult> GetDetailById(Guid logId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out Guid userId))
                return StatusCode(500, "Không xác định được người dùng.");

            var result = await _inventoryLogService
                .GetByIdAsync(logId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(403, result.Message);
        }
    }
}
