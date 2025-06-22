using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoffeeTypeController(ICoffeeTypeService coffeeTypeService) : ControllerBase
    {
        private readonly ICoffeeTypeService _coffeeTypeService = coffeeTypeService;

        // GET: api/<CoffeeType>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,BusinessStaff,Farmer,DeliveryStaff")]
        public async Task<IActionResult> GetAllCoffeeTypesAsync()
        {
            var result = await _coffeeTypeService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<CoffeeType>/{typeId}
        [HttpGet("{typeId}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,BusinessStaff,Farmer,DeliveryStaff")]
        public async Task<IActionResult> GetById(Guid typeId)
        {
            var result = await _coffeeTypeService.GetById(typeId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
