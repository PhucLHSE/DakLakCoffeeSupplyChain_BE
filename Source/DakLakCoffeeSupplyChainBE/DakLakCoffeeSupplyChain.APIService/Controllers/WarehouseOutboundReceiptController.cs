using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundReceiptDTOs;
using DakLakCoffeeSupplyChain.Common;
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

        // POST: api/WarehouseOutboundReceipts/{id}/receipt
        [HttpPost("{id}/receipt")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> CreateReceipt(
            Guid id, 
            [FromBody] WarehouseOutboundReceiptCreateDto dto)
        {
            var staffUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (staffUserIdClaim == null || !Guid.TryParse(staffUserIdClaim.Value, out Guid staffUserId))
                return Unauthorized("Cannot determine user from token.");

            dto.OutboundRequestId = id;

            var result = await _receiptService
                .CreateAsync(staffUserId, dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.ERROR_VALIDATION_CODE)
                return UnprocessableEntity(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/WarehouseOutboundReceipts/{receiptId}/confirm
        [HttpPut("{receiptId}/confirm")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> ConfirmReceipt(
            Guid receiptId, 
            [FromBody] WarehouseOutboundReceiptConfirmDto dto)
        {
            var result = await _receiptService
                .ConfirmReceiptAsync(receiptId, dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = result.Message, receiptId = result.Data });

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_VALIDATION_CODE)
                return UnprocessableEntity(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/WarehouseOutboundReceipts
        [HttpGet]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> GetAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Không thể xác định người dùng từ token.");

            var result = await _receiptService
                .GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Không thể xác định người dùng từ token.");

            var result = await _receiptService.GetByIdAsync(id, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpGet("{id}/summary")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> GetSummary(Guid id)
        {
            var result = await _receiptService.GetSummaryAsync(id);
            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.FAIL_READ_CODE) return NotFound(result.Message);
            return StatusCode(500, result.Message);
        }
    }
}
