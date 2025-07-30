using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentsController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentsController(IShipmentService shipmentService)
            => _shipmentService = shipmentService;

        // GET: api/<ShipmentsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager,BusinessStaff,DeliveryStaff")]
        public async Task<IActionResult> GetAllShipmentsAsync()
        {
            var result = await _shipmentService
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/<ShipmentsController>/{shipmentId}
        [HttpGet("{shipmentId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,DeliveryStaff")]
        public async Task<IActionResult> GetById(Guid shipmentId)
        {
            var result = await _shipmentService
                .GetById(shipmentId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // PATCH: api/<ShipmentsController>/soft-delete/{shipmentId}
        [HttpPatch("soft-delete/{shipmentId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteShipmentByIdAsync(Guid shipmentId)
        {
            var result = await _shipmentService
                .SoftDeleteShipmentById(shipmentId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đơn giao hàng cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> ShipmentExistsAsync(Guid shipmentId)
        {
            var result = await _shipmentService
                .GetById(shipmentId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
