using DakLakCoffeeSupplyChain.Common;
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
            var result = await _service.GetAll();

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
            var result = await _service.GetById(farmerId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }
    }
}
