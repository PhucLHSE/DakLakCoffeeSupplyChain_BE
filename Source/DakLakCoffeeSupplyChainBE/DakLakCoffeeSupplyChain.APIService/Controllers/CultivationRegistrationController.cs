using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CultivationRegistrationController(ICultivationRegistrationService service) : ControllerBase
    {
        private readonly ICultivationRegistrationService _service = service;

        // GET: api/<CultivationRegistration>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetAllCultivationRegistrationnAsync()
        {
            var result = await _service.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<CultivationRegistration>/{registrationId}
        [HttpGet("{registrationId}")]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetById(Guid registrationId)
        {
            var result = await _service.GetById(registrationId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }
    }
}
