using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
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

        // DELETE api/<CoffeeType>/{typeId}
        [HttpDelete("{typeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCoffeeTypeByIdAsync(Guid typeId)
        {
            var result = await _coffeeTypeService.DeleteById(typeId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy coffee type.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<CoffeeType>/soft-delete/{typeId}
        [HttpPatch("soft-delete/{typeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeleteCoffeeTypeByIdAsync(Guid typeId)
        {
            var result = await _coffeeTypeService.SoftDeleteById(typeId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy cofee type.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // POST api/<CoffeeType>
        [HttpPost]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> CreateCoffeeTypeAsync([FromBody] CoffeeTypeCreateDto typeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _coffeeTypeService.Create(typeDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { typeId = ((CoffeeTypeViewAllDto)result.Data).CoffeeTypeId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
