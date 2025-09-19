using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentConfigurationsController : ControllerBase
    {
        private readonly IPaymentConfigurationService _paymentConfigurationService;

        public PaymentConfigurationsController(IPaymentConfigurationService paymentConfigurationService)
            => _paymentConfigurationService = paymentConfigurationService;

        // GET: api/<PaymentConfigurationsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPaymentConfigurationsAsync()
        {
            var result = await _paymentConfigurationService
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/<PaymentConfigurationsController>/{configId}
        [HttpGet("{configId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(Guid configId)
        {
            var result = await _paymentConfigurationService
                .GetById(configId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<PaymentConfigurationsController>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePaymentConfigurationAsync(
            [FromBody] PaymentConfigurationCreateDto paymentConfigurationCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentConfigurationService
                .Create(paymentConfigurationCreateDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { configId = ((PaymentConfigurationViewDetailsDto)result.Data).ConfigId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<PaymentConfigurationsController>/{configId}
        [HttpPut("{configId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderAsync(
            Guid configId,
            [FromBody] PaymentConfigurationUpdateDto paymentConfigurationUpdateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (configId != paymentConfigurationUpdateDto.ConfigId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentConfigurationService
                .Update(paymentConfigurationUpdateDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy loại phí cần cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<PaymentConfigurationsController>/{configId}
        [HttpDelete("{configId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePaymentConfigurationByIdAsync(Guid configId)
        {
            var result = await _paymentConfigurationService
                .DeletePaymentConfigurationById(configId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy loại phí cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<PaymentConfigurationsController>/soft-delete/{configId}
        [HttpPatch("soft-delete/{configId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeletePaymentConfigurationByIdAsync(Guid configId)
        {
            var result = await _paymentConfigurationService
                .SoftDeletePaymentConfigurationById(configId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy loại phí cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<PaymentConfigurationsController>/toggle-status/{configId}
        [HttpPatch("toggle-status/{configId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActiveStatusAsync(Guid configId)
        {
            var result = await _paymentConfigurationService
                .ToggleActiveStatus(configId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        private async Task<bool> PaymentConfigurationExistsAsync(Guid configId)
        {
            var result = await _paymentConfigurationService
                .GetById(configId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
