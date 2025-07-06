using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountsController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public UserAccountsController(IUserAccountService userAccountService)
            => _userAccountService = userAccountService;

        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> GetAllUserAccountsAsync()
        {
            Guid userId;
            string userRole;

            try
            {
                // Lấy userId và userRole từ token qua ClaimsHelper
                userId = User.GetUserId();
                userRole = User.GetRole();
            }
            catch
            {
                return Unauthorized("Không xác định được userId hoặc role từ token.");
            }

            var result = await _userAccountService.GetAll(userId, userRole);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);         

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     

            return StatusCode(500, result.Message); 
        }

        // GET api/<UserAccountsController>/{userId}
        [HttpGet("{userId}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,BusinessStaff,Farmer,DeliveryStaff")]
        public async Task<IActionResult> GetById(Guid userId)
        {
            var result = await _userAccountService.GetById(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     

            return StatusCode(500, result.Message);  
        }

        // POST api/<UserAccountsController>
        [HttpPost]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> CreateUserAccountAsync([FromBody] UserAccountCreateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState); 

            var result = await _userAccountService.Create(userDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), 
                    new { userId = ((UserAccountViewDetailsDto)result.Data).UserId }, 
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message); 

            return StatusCode(500, result.Message);
        }

        // PUT api/<UserAccountsController>/{userId}
        [HttpPut("{userId}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,BusinessStaff,Farmer,DeliveryStaff")]
        public async Task<IActionResult> UpdateUserAccountAsync(Guid userId, [FromBody] UserAccountUpdateDto userDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (userId != userDto.UserId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userAccountService.Update(userDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy người dùng để cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<UserAccountsController>/{userId}
        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> DeleteUserAccountByIdAsync(Guid userId)
        {
            var result = await _userAccountService.DeleteById(userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy người dùng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<UserAccountsController>/soft-delete/{userId}
        [HttpPatch("soft-delete/{userId}")]
        [Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,Farmer")]
        public async Task<IActionResult> SoftDeleteUserAccountByIdAsync(Guid userId)
        {
            var result = await _userAccountService.SoftDeleteById(userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy người dùng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> UserAccountExistsAsync(Guid userId)
        {
            var result = await _userAccountService.GetById(userId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
