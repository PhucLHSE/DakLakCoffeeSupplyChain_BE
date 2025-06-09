using DakLakCoffeeSupplyChain.Common.DTOs.Flow4DTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class WarehouseReceiptController : ControllerBase
    {
        private readonly IWarehouseReceiptService _warehouseReceiptService;

        public WarehouseReceiptController(IWarehouseReceiptService warehouseReceiptService)
        {
            _warehouseReceiptService = warehouseReceiptService;
        }

        [HttpPost]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> CreateReceipt([FromBody] WarehouseReceiptCreateDto dto)
        {
            var staffUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            if (staffUserId == Guid.Empty)
            {
                var errorResult = new ServiceResult(401, "Không xác định được người dùng.");
                return StatusCode(errorResult.Status, errorResult);
            }

            var result = await _warehouseReceiptService.CreateWarehouseReceiptAsync(dto, staffUserId);
            return StatusCode(result.Status, result);
        }
        [HttpGet]
        [Authorize(Roles = "BusinessStaff,Administrator")]
        public async Task<IActionResult> GetAllReceipts()
        {
            var result = await _warehouseReceiptService.GetAllReceiptsAsync();
            return StatusCode(result.Status, result);
        }
    }
}