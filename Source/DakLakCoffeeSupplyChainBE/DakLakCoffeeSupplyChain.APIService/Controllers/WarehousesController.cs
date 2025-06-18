using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehousesController : Controller
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost]
        [Authorize(Roles = "BusinessManager,Admin")]
        public async Task<IActionResult> Create([FromBody] WarehouseCreateDto dto)
        {
            var result = await _warehouseService.CreateAsync(dto);
            return StatusCode(result.Status, result);
        }
        [HttpGet]
        [Authorize(Roles = "Admin, BusinessManager")]
        public async Task<IActionResult> GetAllWarehouses()
        {
            var result = await _warehouseService.GetAllAsync();
            return StatusCode(result.Status, result);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] WarehouseUpdateDto dto)
        {
            var result = await _warehouseService.UpdateAsync(id, dto);
            return StatusCode(result.Status, result);
        }

    }
}
