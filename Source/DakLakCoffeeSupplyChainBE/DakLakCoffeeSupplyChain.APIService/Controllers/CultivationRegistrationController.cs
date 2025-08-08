using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcurementPlanDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CultivationRegistrationController(ICultivationRegistrationService service) : ControllerBase
    {
        private readonly ICultivationRegistrationService _service = service;

        // GET: api/<CultivationRegistration>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager, Farmer")]
        public async Task<IActionResult> GetAllCultivationRegistrationnAsync()
        {
            var result = await _service
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET: api/<CultivationRegistration/Available>
        [HttpGet("Available/{planId}")]
        [EnableQuery]
        public async Task<IActionResult> GetAllCultivationRegistrationnAvailableAsync(Guid planId)
        {
            var result = await _service
                .GetAllAvailable(planId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<CultivationRegistration>/{registrationId}
        [HttpGet("{registrationId}")]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager, Farmer")]
        public async Task<IActionResult> GetById(Guid registrationId)
        {
            var result = await _service
                .GetById(registrationId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // DELETE api/<CultivationRegistration>/{registrationId}
        [HttpDelete("{registrationId}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> DeleteByIdAsync(Guid registrationId)
        {
            var result = await _service
                .DeleteById(registrationId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy cultivation registration.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<CultivationRegistration>/soft-delete/{registrationId}
        [HttpPatch("soft-delete/{registrationId}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> SoftDeleteByIdAsync(Guid registrationId)
        {
            var result = await _service
                .SoftDeleteById(registrationId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy cultivation registration.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // POST api/<CultivationRegistration>
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CreateCultivationRegistrationAsync(
            [FromBody] CultivationRegistrationCreateViewDto registrationId)
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

            var result = await _service
                .Create(registrationId, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { registrationId = ((CultivationRegistrationViewSumaryDto)result.Data).RegistrationId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH api/<CultivationRegistration>/UpdateStatus/{registrationDetailId}
        [HttpPatch("Detail/UpdateStatus/{registrationDetailId}")]
        [Authorize(Roles = "BusinessManager, Farmer")]
        public async Task<IActionResult> UpdateStatusAsync(Guid registrationDetailId,
            [FromBody] CultivationRegistrationUpdateStatusDto updateDto)
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

            var result = await _service
                .UpdateStatus(updateDto, userId, registrationDetailId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đơn đăng ký.");

            return StatusCode(500, result.Message);
        }
    }
}
