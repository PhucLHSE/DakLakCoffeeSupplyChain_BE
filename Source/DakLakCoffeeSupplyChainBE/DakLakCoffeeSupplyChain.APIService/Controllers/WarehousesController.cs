using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
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
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        // POST: api/Warehouses
        [HttpPost]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> Create([FromBody] WarehouseCreateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized(new { message = "Cannot identify BusinessManager from token." });

            var result = await _warehouseService.CreateAsync(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // GET: api/Warehouses
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin, BusinessManager, BusinessStaff")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized(new { message = "Cannot identify user from token." });

            var result = await _warehouseService.GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return StatusCode(403, new { message = result.Message }); // ✅ FIXED: no more Forbid(message)

            return StatusCode(500, new { message = result.Message });
        }

        // GET: api/Warehouses/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, BusinessManager, BusinessStaff")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _warehouseService.GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // PUT: api/Warehouses/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] WarehouseUpdateDto dto)
        {
            var result = await _warehouseService.UpdateAsync(id, dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(new { message = result.Message });

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // DELETE (soft): api/Warehouses/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _warehouseService.DeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.FAIL_READ_CODE)
                return Conflict(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // DELETE (hard): api/Warehouses/hard-delete/{id}
        [HttpDelete("hard-delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _warehouseService.HardDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.FAIL_READ_CODE)
                return Conflict(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }
    }
}
