using DakLakCoffeeSupplyChain.Common;
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
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Không xác định được UserId từ token.");

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
    }
}
