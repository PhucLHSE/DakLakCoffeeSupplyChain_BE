using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoriesController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // GET: api/Inventories
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> GetAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return StatusCode(500, "Không xác định được người dùng từ token.");

            var result = await _inventoryService
                .GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return Forbid(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/Inventories/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _inventoryService
                .GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // POST: api/Inventories
        [HttpPost]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> Create(
            [FromBody] InventoryCreateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return StatusCode(500, "Không xác định được người dùng từ token.");

            var result = await _inventoryService.CreateAsync(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH: api/Inventories/soft-delete/{id}
        [HttpPatch("soft-delete/{id}")]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _inventoryService
                .SoftDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy bản ghi tồn kho.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }


        // DELETE (Hard): api/Inventories/hard/{id}
        [HttpDelete("hard/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _inventoryService
                .HardDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_DELETE_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/Inventories/warehouse/{warehouseId}
        [HttpGet("warehouse/{warehouseId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetByWarehouseId(Guid warehouseId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _inventoryService
                .GetAllByWarehouseIdAsync(warehouseId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/Inventories/warehouse/{warehouseId}/fifo
        [HttpGet("warehouse/{warehouseId}/fifo")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetByWarehouseIdWithFifo(
            Guid warehouseId, [FromQuery] double? requestedQuantity = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _inventoryService
                .GetInventoriesWithFifoRecommendationAsync(warehouseId, userId, requestedQuantity);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        
        // GET: api/Inventories/warehouse/{warehouseId}/detail
        [HttpGet("warehouse/{warehouseId}/detail")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetByWarehouseIdForDetail(Guid warehouseId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _inventoryService
                .GetAllByWarehouseIdForDetailAsync(warehouseId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
