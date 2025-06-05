using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

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

        // GET: api/<UserAccountsController>
        [HttpGet]
        public async Task<IActionResult> GetAllUserAccountsAsync()
        {
            var result = await _userAccountService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<UserAccountsController>/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetById(Guid userId)
        {
            var result = await _userAccountService.GetById(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<UserAccountsController>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserAccountCreateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState); // Validate thất bại

            var result = await _userAccountService.Create(userDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { userId = ((UserAccountViewDetailsDto)result.Data).UserId }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message); // Trùng email/phone...

            return StatusCode(500, result.Message);
        }
    }
}
