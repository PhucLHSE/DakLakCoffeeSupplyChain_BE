using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
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
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
            => _orderService = orderService;

        // GET: api/<OrdersController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetAllOrdersAsync()
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

            var result = await _orderService
                .GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/<OrdersController>/{orderId}
        [HttpGet("{orderId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetById(Guid orderId)
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

            var result = await _orderService
                .GetById(orderId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<OrdersController>
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> CreateOrderAsync(
            [FromBody] OrderCreateDto orderCreateDto)
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

            var result = await _orderService.Create(orderCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { orderId = ((OrderViewDetailsDto)result.Data).OrderId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<OrdersController>/{orderId}
        [HttpPut("{orderId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> UpdateOrderAsync(
            Guid orderId,
            [FromBody] OrderUpdateDto orderUpdateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (orderId != orderUpdateDto.OrderId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _orderService.Update(orderUpdateDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đợt đơn hàng.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<OrdersController>/{orderId}
        [HttpDelete("{orderId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> DeleteOrderByIdAsync(Guid orderId)
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

            var result = await _orderService
                .DeleteOrderById(orderId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đơn hàng cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<OrdersController>/soft-delete/{orderId}
        [HttpPatch("soft-delete/{orderId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteOrderByIdAsync(Guid orderId)
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

            var result = await _orderService
                .SoftDeleteOrderById(orderId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đơn hàng cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> OrderExistsAsync(Guid orderId)
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return false;
            }

            var result = await _orderService
                .GetById(orderId, userId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
