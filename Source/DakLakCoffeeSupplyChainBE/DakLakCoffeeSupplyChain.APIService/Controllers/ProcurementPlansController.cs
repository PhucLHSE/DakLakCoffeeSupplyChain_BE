using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcurementPlansController : ControllerBase
    {
        private readonly IProcurementPlanService _procurementPlanService;

        public ProcurementPlansController(IProcurementPlanService procurementPlanService)
            => _procurementPlanService = procurementPlanService;

        // GET: api/<ProcurementPlans>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetAllProcurementPlansAsync()
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _procurementPlanService
                .GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // Chỉ những plan nào đang mở (status == open), dành cho mọi người
        // GET: api/<ProcurementPlans/Available>
        [HttpGet("Available")]
        public async Task<IActionResult> GetAllProcurementPlansAvailableAsync()
        {
            var result = await _procurementPlanService
                .GetAllProcurementPlansAvailable();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<ProcurementPlans>/{planId}
        [HttpGet("{planId}")]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetById(Guid planId)
        {
            var result = await _procurementPlanService
                .GetById(planId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // GET api/<ProcurementPlans>/Available/{planId}
        [HttpGet("Available/{planId}")]
        public async Task<IActionResult> GetByIdExceptDisablePlanDetails(Guid planId)
        {
            var result = await _procurementPlanService
                .GetByIdExceptDisablePlanDetails(planId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // DELETE api/<ProcurementPlans>/{planId}
        //[HttpDelete("{planId}")]
        //public async Task<IActionResult> DeleteProcurementPlanByIdAsync(Guid planId)
        //{
        //    var result = await _procurementPlanService.DeleteById(planId);

        //    if (result.Status == Const.SUCCESS_DELETE_CODE)
        //        return Ok("Xóa thành công.");

        //    if (result.Status == Const.WARNING_NO_DATA_CODE)
        //        return NotFound("Không tìm thấy kế hoạch.");

        //    if (result.Status == Const.FAIL_DELETE_CODE)
        //        return Conflict("Xóa thất bại.");

        //    return StatusCode(500, result.Message);
        //}

        // PATCH: api/<ProductsController>/soft-delete/{productId}
        [HttpPatch("soft-delete/{planId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteProcurementPlanByIdAsync(Guid planId)
        {
            var result = await _procurementPlanService
                .SoftDeleteById(planId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return NoContent();

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy kế hoạch.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // POST api/<ProcurementPlan>
        [HttpPost]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CreateProcurementPlanAsync(
            [FromBody] ProcurementPlanCreateDto planDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _procurementPlanService
                .Create(planDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE && result.Data is ProcurementPlanViewDetailsSumaryDto createdDto)
                return CreatedAtAction(nameof(GetById),
                    new { planId = createdDto.PlanId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH api/<ProcurementPlan>/Update/{planId}
        [HttpPatch("Update/{planId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> UpdateAsync(Guid planId,
            [FromBody] ProcurementPlanUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _procurementPlanService
                .Update(updateDto, userId, planId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy kế hoạch.");

            return StatusCode(500, result.Message);
        }

        // PATCH api/<ProcurementPlan>/UpdateStatus/{planId}
        [HttpPatch("UpdateStatus/{planId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> UpdateStatusAsync(Guid planId,
            [FromBody] ProcurementPlanUpdateStatusDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _procurementPlanService
                .UpdateStatus(updateDto, userId, planId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy kế hoạch.");

            return StatusCode(500, result.Message);
        }

        // GET api/<ProcurementPlans>/{planId}/payment-status
        [HttpGet("{planId}/payment-status")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CheckPaymentStatusAsync(Guid planId)
        {
            var result = await _procurementPlanService.CheckPaymentStatus(planId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
