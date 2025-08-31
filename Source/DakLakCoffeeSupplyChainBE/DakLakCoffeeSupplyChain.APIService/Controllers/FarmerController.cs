using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.FarmerDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmerController (IFarmerService farmerService): ControllerBase
    {
        private readonly IFarmerService _service = farmerService;

        // GET: api/<Farmer>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _service
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<Farmer>/{farmerId}
        [HttpGet("{farmerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(Guid farmerId)
        {
            var result = await _service
                .GetById(farmerId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // DELETE api/<Farmer>/{farmerId}
        [HttpDelete("{farmerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteByIdAsync(Guid farmerId)
        {
            var result = await _service
                .DeleteById(farmerId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy nông dân.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<Farmer>/soft-delete/{farmerId}
        [HttpPatch("soft-delete/{farmerId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeleteByIdAsync(Guid farmerId)
        {
            var result = await _service
                .SoftDeleteById(farmerId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy nông dân.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<Farmer>/{farmerId}/verify
        [HttpPatch("{farmerId}/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyFarmer(Guid farmerId, [FromBody] FarmerVerifyDto verifyDto)
        {
            var result = await _service.VerifyFarmer(farmerId, verifyDto.IsVerified);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok("Cập nhật trạng thái xác thực thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy nông dân.");

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict("Cập nhật trạng thái xác thực thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
