using DakLakCoffeeSupplyChain.Common.DTOs.InventoryDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
        [Authorize(Roles = "BusinessStaff,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _inventoryService.GetAllAsync();
            return StatusCode(result.Status, result);
        }

        /// <summary>
        /// Lấy chi tiết tồn kho theo ID (view detail)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff,Admin")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _inventoryService.GetByIdAsync(id);
            return StatusCode(result.Status, result);
        }
        [HttpPost]
        [Authorize(Roles = "BusinessStaff,Admin")]
        public async Task<IActionResult> Create([FromBody] InventoryCreateDto dto)
        {
            var result = await _inventoryService.CreateAsync(dto);
            return StatusCode(result.Status, result);
        }
        /// <summary>
        /// Xoá mềm tồn kho (IsDeleted = true)
        /// </summary>
        [HttpDelete("soft/{id}")]
        [Authorize(Roles = "BusinessStaff,Admin")]
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
