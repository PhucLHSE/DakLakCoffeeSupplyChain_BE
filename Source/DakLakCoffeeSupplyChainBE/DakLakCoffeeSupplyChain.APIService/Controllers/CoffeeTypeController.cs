using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
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
            var result = await _coffeeTypeService
                .GetAll();

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
            var result = await _coffeeTypeService
                .GetById(typeId);

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
            var result = await _coffeeTypeService
                .DeleteById(typeId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return NoContent();

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
            var result = await _coffeeTypeService
                .SoftDeleteById(typeId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return NoContent();

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy cofee type.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        // POST api/<CoffeeType>
        [HttpPost]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> CreateCoffeeTypeAsync(
            [FromBody] CoffeeTypeCreateDto typeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _coffeeTypeService
                .Create(typeDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE && result.Data is CoffeeTypeViewAllDto createdDto)
                return CreatedAtAction(nameof(GetById),
                    new { typeId = createdDto.CoffeeTypeId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<CoffeeType>/{typeId}
        [HttpPut("{typeId}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> UpdateCoffeeTypeAsync(
            Guid typeId, 
            [FromBody] CoffeeTypeUpdateDto typeDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (typeId != typeDto.CoffeeTypeId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _coffeeTypeService
                .Update(typeDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy coffee type để cập nhật.");

            return StatusCode(500, result.Message);
        }

        // PUT api/<CoffeeType>/status/{typeId}
        [HttpPatch("status/{typeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCoffeeTypeStatusAsync(
            Guid typeId,
            [FromBody] CoffeeTypeUpdateStatusDto typeDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (typeId != typeDto.CoffeeTypeId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _coffeeTypeService
                .UpdateStatus(typeDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy coffee type để cập nhật.");

            return StatusCode(500, result.Message);
        }
    }
}
