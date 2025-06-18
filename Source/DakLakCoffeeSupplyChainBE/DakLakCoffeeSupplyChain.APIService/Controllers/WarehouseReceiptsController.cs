using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseReceiptsController : ControllerBase
    {
        private readonly IWarehouseReceiptService _receiptService;

        public WarehouseReceiptsController(IWarehouseReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

      
        [HttpPost("{id}/receipt")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> CreateReceipt(Guid id, [FromBody] WarehouseReceiptCreateDto dto)
        {
            var staffUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            dto.InboundRequestId = id;

            var result = await _receiptService.CreateReceiptAsync(staffUserId, dto);
            return StatusCode(result.Status, result);
        }
        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> ConfirmReceipt(Guid id)
        {
            var result = await _receiptService.ConfirmReceiptAsync(id);
            return StatusCode(result.Status, result);
        }
    }
}
