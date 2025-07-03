using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
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

        /// <summary>
        /// Lấy danh sách tồn kho (view all)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> GetAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Không xác định được người dùng từ token.");

            var result = await _inventoryService.GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data); // 200

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); // 404

            if (result.Status == Const.FAIL_READ_CODE)
                return Forbid(result.Message); // 403

            return StatusCode(500, result.Message); // fallback
        }
        /// <summary>
        /// Lấy chi tiết tồn kho theo ID (view detail)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _inventoryService.GetByIdAsync(id);
            return StatusCode(result.Status, result);
        }
        [HttpPost]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> Create([FromBody] InventoryCreateDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _inventoryService.CreateAsync(dto, userId);
            return StatusCode(result.Status, result);
        }
        /// <summary>
        /// Xoá mềm tồn kho (IsDeleted = true)
        /// </summary>
        [HttpDelete("soft/{id}")]
        [Authorize(Roles = "BusinessStaff,Admin,BusinessManager")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _inventoryService.SoftDeleteAsync(id);
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Xoá thật tồn kho khỏi DB
        /// </summary>
        [HttpDelete("hard/{id}")]
        [Authorize(Roles = "Admin")] // Xoá thật thì giới hạn cho Admin
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _inventoryService.HardDeleteAsync(id);
            return StatusCode(result.Status, result);
        }
    }
}
