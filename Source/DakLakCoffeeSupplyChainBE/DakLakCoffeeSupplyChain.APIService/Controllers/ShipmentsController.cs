using DakLakCoffeeSupplyChain.Common;
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
    }
}
