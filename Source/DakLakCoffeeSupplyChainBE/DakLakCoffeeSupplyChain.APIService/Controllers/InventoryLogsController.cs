using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            try
            {
                var result = await _inventoryLogService.GetAllAsync();

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                if (result.Status == Const.FAIL_READ_CODE)
                    return Forbid(result.Message);

                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Đã xảy ra lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
