using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseReceiptDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;
using DakLakCoffeeSupplyChain.Common.Helpers;

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

        // POST: api/WarehouseReceipts/{id}/receipt
        [HttpPost("{id}/receipt")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> CreateReceipt(
            Guid id, 
            [FromBody] WarehouseReceiptCreateDto dto)
        {
            var staffUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (staffUserIdClaim == null || !Guid.TryParse(staffUserIdClaim.Value, out Guid staffUserId))
                return Unauthorized("Cannot determine user from token.");

            dto.InboundRequestId = id;

            var result = await _receiptService
                .CreateReceiptAsync(staffUserId, dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.ERROR_VALIDATION_CODE)
                return UnprocessableEntity(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/WarehouseReceipts/{id}/confirm
        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> ConfirmReceipt(
            Guid id, 
            [FromBody] WarehouseReceiptConfirmDto dto)
        {
            var result = await _receiptService
                .ConfirmReceiptAsync(id, dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = result.Message, receiptId = result.Data });

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_VALIDATION_CODE)
                return UnprocessableEntity(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/WarehouseReceipts
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin,BusinessStaff,BusinessManager")]
        public async Task<IActionResult> GetAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _receiptService
                .GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return Forbid(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/WarehouseReceipts/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,BusinessStaff,BusinessManager")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _receiptService
                .GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH: api/WarehouseReceipts/soft-delete/{id}
        [HttpPatch("soft-delete/{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> SoftDeleteAsync(Guid id)
        {
            Guid currentUserId;
            string currentUserRole;

            try
            {
                // Lấy userId và userRole từ token qua ClaimsHelper
                currentUserId = User.GetUserId();
                currentUserRole = User.GetRole();
            }
            catch
            {
                return Unauthorized("Không xác định được userId hoặc role từ token.");
            }

            // (Optional) Kiểm tra quyền nếu cần: 
            // var canAccess = await _receiptService.CanAccess(currentUserId, currentUserRole, id);
            // if (!canAccess)
            //     return StatusCode(403, "Bạn không có quyền xóa phiếu này.");

            var result = await _receiptService
                .SoftDeleteAsync(id, currentUserId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy phiếu.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // DELETE: api/WarehouseReceipts/{id}/hard
        [HttpDelete("{id}/hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var result = await _receiptService
                .HardDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/WarehouseReceipts/debug
        [HttpGet("debug")]
        [Authorize(Roles = "Admin,BusinessStaff,BusinessManager")]
        public async Task<IActionResult> GetDebugInfo()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _receiptService.GetAllAsync(userId);
            
            if (result.Status == Const.SUCCESS_READ_CODE && result.Data is List<object> receipts)
            {
                var debugInfo = new
                {
                    TotalReceipts = receipts.Count,
                    FreshCoffee = receipts.Count(r => 
                        ((dynamic)r).DetailId != null && ((dynamic)r).BatchId == null),
                    ProcessedCoffee = receipts.Count(r => 
                        ((dynamic)r).BatchId != null && ((dynamic)r).DetailId == null),
                    Unknown = receipts.Count(r => 
                        (((dynamic)r).BatchId == null && ((dynamic)r).DetailId == null) || 
                        (((dynamic)r).BatchId != null && ((dynamic)r).DetailId != null))
                };
                
                return Ok(new { status = 1, message = "Debug info", data = debugInfo });
            }
            
            return Ok(result);
        }
    }
}
