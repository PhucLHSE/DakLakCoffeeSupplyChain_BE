using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAllRolesAsync()
        {
            var result = await _roleService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<RolesController>/{roleId}
        [HttpGet("{roleId}")]
        public async Task<IActionResult> GetById(int roleId)
        {
            var result = await _roleService.GetById(roleId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // DELETE api/<RolesController>/{roleId}
        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRoleByIdAsync(int roleId)
        {
            var result = await _roleService.DeleteById(roleId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy người dùng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> RoleExistsAsync(int roleId)
        {
            var result = await _roleService.GetById(roleId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
