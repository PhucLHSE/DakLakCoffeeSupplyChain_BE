using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController : ControllerBase
    {
        private readonly IOrderItemService _orderItemService;

        public OrderItemsController(IOrderItemService orderItemService)
            => _orderItemService = orderItemService;

        // POST api/<OrderItemsController>
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> CreateOrderItemAsync(
            [FromBody] OrderItemCreateDto orderItemCreateDto)
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

            var result = await _orderItemService
                .Create(orderItemCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(201, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<OrderItemsController>/{orderItemId}
        [HttpPut("{orderItemId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> UpdateContractItemAsync(
            Guid orderItemId, 
            [FromBody] OrderItemUpdateDto orderItemUpdateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (orderItemId != orderItemUpdateDto.OrderItemId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _orderItemService
                .Update(orderItemUpdateDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy mục đơn hàng cần cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<OrderItemsController>/{orderItemId}
        [HttpDelete("{orderItemId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> DeleteOrderItemByIdAsync(Guid orderItemId)
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

            var result = await _orderItemService
                .DeleteOrderItemById(orderItemId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy mục hàng trong đơn hàng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<OrderItemsController>/soft-delete/{orderItemId}
        [HttpPatch("soft-delete/{orderItemId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteOrderItemByIdAsync(Guid orderItemId)
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

            var result = await _orderItemService
                .SoftDeleteOrderItemById(orderItemId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đơn hàng thuộc hợp đồng với mã được cung cấp.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
