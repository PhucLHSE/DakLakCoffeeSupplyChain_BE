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
    }
}
