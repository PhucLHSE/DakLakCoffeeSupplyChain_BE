using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ShipmentDTOs.ShipmentDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentDetailsController : ControllerBase
    {
        private readonly IShipmentDetailService _shipmentDetailService;

        public ShipmentDetailsController(IShipmentDetailService shipmentDetailService)
            => _shipmentDetailService = shipmentDetailService;

        // POST api/<ShipmentDetailsController>
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> CreateShipmentDetailAsync(
            [FromBody] ShipmentDetailCreateDto shipmentDetailCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shipmentDetailService
                .Create(shipmentDetailCreateDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(201, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<ShipmentDetailsController>/{shipmentDetailId}
        [HttpPut("{shipmentDetailId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> UpdateShipmentDetailAsync(
            Guid shipmentDetailId,
            [FromBody] ShipmentDetailUpdateDto shipmentDetailUpdateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (shipmentDetailId != shipmentDetailUpdateDto.ShipmentDetailId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _shipmentDetailService
                .Update(shipmentDetailUpdateDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy chi tiết đơn giao hàng cần cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<ShipmentDetailsController>/{shipmentDetailId}
        [HttpDelete("{shipmentDetailId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> DeleteShipmentDetailByIdAsync(Guid shipmentDetailId)
        {
            var result = await _shipmentDetailService
                .DeleteShipmentDetailById(shipmentDetailId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Chi tiết đơn giao hàng không tồn tại hoặc đã bị xoá.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<ShipmentDetailsController>/soft-delete/{shipmentDetailId}
        [HttpPatch("soft-delete/{shipmentDetailId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteShipmentDetailByIdAsync(Guid shipmentDetailId)
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

            var result = await _shipmentDetailService
                .SoftDeleteShipmentDetailById(shipmentDetailId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Chi tiết đơn giao hàng không tồn tại hoặc đã bị xoá.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
