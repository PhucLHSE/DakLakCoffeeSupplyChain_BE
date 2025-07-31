using DakLakCoffeeSupplyChain.Common;
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

        // PATCH: api/<ShipmentDetailsController>/soft-delete/{shipmentDetailId}
        [HttpPatch("soft-delete/{shipmentDetailId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteShipmentDetailByIdAsync(Guid shipmentDetailId)
        {
            var result = await _shipmentDetailService
                .SoftDeleteShipmentDetailById(shipmentDetailId);

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
