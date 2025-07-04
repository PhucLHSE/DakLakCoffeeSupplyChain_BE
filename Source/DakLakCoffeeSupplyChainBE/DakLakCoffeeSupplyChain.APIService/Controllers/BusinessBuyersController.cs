using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "BusinessManager")]
    public class BusinessBuyersController : ControllerBase
    {
        private readonly IBusinessBuyerService _businessBuyerService;

        public BusinessBuyersController(IBusinessBuyerService businessBuyerService)
            => _businessBuyerService = businessBuyerService;

        // GET: api/<BusinessBuyersController>
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllBussinessBuyersAsync()
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

            var result = await _businessBuyerService.GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<BusinessBuyersController>/{buyerId}
        [HttpGet("{buyerId}")]
        public async Task<IActionResult> GetById(Guid buyerId)
        {
            var result = await _businessBuyerService.GetById(buyerId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<BusinessBuyersController>
        [HttpPost]
        public async Task<IActionResult> CreateBusinessBuyerAsync([FromBody] BusinessBuyerCreateDto businessBuyerCreateDto)
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

            var result = await _businessBuyerService.Create(businessBuyerCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { buyerId = ((BusinessBuyerViewDetailDto)result.Data).BuyerId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<BusinessBuyersController>/{buyerId}
        [HttpPut("{buyerId}")]
        public async Task<IActionResult> UpdateBusinessBuyerAsync(Guid buyerId, [FromBody] BusinessBuyerUpdateDto businessBuyerDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (buyerId != businessBuyerDto.BuyerId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

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

            var result = await _businessBuyerService.Update(businessBuyerDto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy khách hàng để cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<BusinessBuyersController>/{buyerId}
        [HttpDelete("{buyerId}")]
        public async Task<IActionResult> DeleteBusinessBuyerByIdAsync(Guid buyerId)
        {
            var result = await _businessBuyerService.DeleteBusinessBuyerById(buyerId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy khách hàng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<BusinessBuyersController>/soft-delete/{buyerId}
        [HttpPatch("soft-delete/{buyerId}")]
        public async Task<IActionResult> SoftDeleteBusinessBuyerByIdAsync(Guid buyerId)
        {
            var result = await _businessBuyerService.SoftDeleteBusinessBuyerById(buyerId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy khách hàng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> BusinessBuyerExistsAsync(Guid buyerId)
        {
            var result = await _businessBuyerService.GetById(buyerId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
