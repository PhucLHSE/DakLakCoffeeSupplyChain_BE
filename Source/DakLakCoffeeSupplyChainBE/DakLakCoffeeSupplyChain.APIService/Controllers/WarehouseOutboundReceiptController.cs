using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseOutboundReceiptsController : ControllerBase
    {
        private readonly IWarehouseOutboundReceiptService _receiptService;

        public WarehouseOutboundReceiptsController(IWarehouseOutboundReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

        [HttpPost("{id}/receipt")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> CreateReceipt(Guid id, [FromBody] WarehouseOutboundReceiptCreateDto dto)
        {
            var staffUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            dto.OutboundRequestId = id;

            var result = await _receiptService.CreateAsync(staffUserId, dto);
            return StatusCode(result.Status, result);
        }

        // [HttpPut("{id}/confirm")] // Nếu sau này có confirm, bạn có thể thêm tại đây
    }
}
