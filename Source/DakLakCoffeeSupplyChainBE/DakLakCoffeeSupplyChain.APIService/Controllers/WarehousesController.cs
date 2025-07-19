using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

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
            Guid userId;

            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _warehouseService.CreateAsync(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/Warehouses
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin,BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            Guid userId;

            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _warehouseService.GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return Forbid(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/Warehouses/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _warehouseService.GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/Warehouses/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> Update(Guid id, [FromBody] WarehouseUpdateDto dto)
        {
            var result = await _warehouseService.UpdateAsync(id, dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH: api/Warehouses/soft-delete/{id}
        [HttpPatch("soft-delete/{id}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _warehouseService.DeleteAsync(id); // Soft delete

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.FAIL_READ_CODE)
                return Conflict("Xóa mềm thất bại: " + result.Message);

            return StatusCode(500, result.Message);
        }

        // DELETE: api/Warehouses/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _warehouseService.HardDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa vĩnh viễn thành công.");

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.FAIL_READ_CODE)
                return Conflict("Xóa thất bại: " + result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
