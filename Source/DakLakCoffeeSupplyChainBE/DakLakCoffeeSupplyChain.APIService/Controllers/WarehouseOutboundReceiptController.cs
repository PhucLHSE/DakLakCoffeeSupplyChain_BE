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

        [HttpPut("{receiptId}/confirm")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> ConfirmReceipt(Guid receiptId, [FromBody] WarehouseOutboundReceiptConfirmDto dto)
        {
            var result = await _receiptService.ConfirmReceiptAsync(receiptId, dto);
            return StatusCode(result.Status, result);
        }
        [HttpGet]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _receiptService.GetAllAsync();
            return StatusCode(result.Status, result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _receiptService.GetByIdAsync(id);
            return StatusCode(result.Status, result);
        }

    }
}
