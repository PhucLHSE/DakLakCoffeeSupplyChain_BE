using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
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
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
            => _roleService = roleService;

        // GET: api/<RolesController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRolesAsync()
        {
            var result = await _roleService
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET: api/<RolesController>
        [HttpGet("BusinessAndFarmer")]
        [EnableQuery]
        public async Task<IActionResult> GetAllRolesBusinessAndFarmerAsync()
        {
            var result = await _roleService
                .GetBusinessAndFarmerRole();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<RolesController>/{roleId}
        [HttpGet("{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int roleId)
        {
            var result = await _roleService
                .GetById(roleId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<RolesController>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRoleAsync(
            [FromBody] RoleCreateDto roleDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _roleService
                .Create(roleDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), 
                    new { roleId = ((RoleViewDetailsDto)result.Data).RoleId }, 
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<RolesController>/{roleId}
        [HttpPut("{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoleAsync(
            int roleId, 
            [FromBody] RoleUpdateDto roleDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (roleId != roleDto.RoleId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _roleService
                .Update(roleDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy vai trò để cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<RolesController>/{roleId}
        [HttpDelete("{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoleByIdAsync(int roleId)
        {
            var result = await _roleService
                .DeleteRoleById(roleId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy vai trò.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<RolesController>/soft-delete/{roleId}
        [HttpPatch("soft-delete/{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeleteRoleByIdAsync(int roleId)
        {
            var result = await _roleService
                .SoftDeleteRoleById(roleId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy vai trò.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> RoleExistsAsync(int roleId)
        {
            var result = await _roleService
                .GetById(roleId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
