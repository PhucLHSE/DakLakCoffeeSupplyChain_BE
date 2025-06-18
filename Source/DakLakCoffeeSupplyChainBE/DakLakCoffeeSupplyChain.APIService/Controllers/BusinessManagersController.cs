using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
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
    public class BusinessManagersController : ControllerBase
    {
        private readonly IBusinessManagerService _businessManagerService;

        public BusinessManagersController(IBusinessManagerService businessManagerService)
            => _businessManagerService = businessManagerService;

        // GET: api/<BusinessManagersController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBussinessManagersAsync()
        {
            var result = await _businessManagerService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<BusinessManagersController>/{managerId}
        [HttpGet("{managerId}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> GetById(Guid managerId)
        {
            var result = await _businessManagerService.GetById(managerId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<BusinessManagersController>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBusinessManagerAsync(
            [FromBody] BusinessManagerCreateDto businessManagerCreateDto, 
            [FromQuery] Guid userId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _businessManagerService.Create(businessManagerCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), 
                    new { managerId = ((BusinessManagerViewDetailsDto)result.Data).ManagerId }, 
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<BusinessManagersController>/soft-delete/{managerId}
        [HttpPatch("soft-delete/{managerId}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> SoftDeleteBusinessManagerByIdAsync(Guid managerId)
        {
            var result = await _businessManagerService.SoftDeleteById(managerId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy quản lý doanh nghiệp.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> BusinessManagerExistsAsync(Guid managerId)
        {
            var result = await _businessManagerService.GetById(managerId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
