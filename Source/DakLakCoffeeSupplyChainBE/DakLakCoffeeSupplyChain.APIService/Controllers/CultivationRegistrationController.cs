using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CultivationRegistrationDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
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
            var result = await _service.GetAll();

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
            var result = await _service.GetById(registrationId);

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
            var result = await _service.DeleteById(registrationId);

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
            var result = await _service.SoftDeleteById(registrationId);

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
        public async Task<IActionResult> CreateCultivationRegistrationAsync([FromBody] CultivationRegistrationCreateViewDto registrationId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.Create(registrationId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { registrationId = ((CultivationRegistrationViewSumaryDto)result.Data).RegistrationId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
