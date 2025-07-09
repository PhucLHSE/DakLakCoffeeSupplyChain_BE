using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = "BusinessStaff,BusinessManager,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out Guid userId))
                return StatusCode(500, "Không xác định được người dùng.");

            var result = await _inventoryLogService.GetAllAsync(userId);

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

            var result = await _inventoryLogService.GetLogsByInventoryIdAsync(inventoryId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(403, result.Message); // Nếu bị chặn quyền, trả 403
        }
    }
}
