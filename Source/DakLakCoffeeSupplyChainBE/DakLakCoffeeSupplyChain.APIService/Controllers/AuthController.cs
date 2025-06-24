using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            return StatusCode(result.Status, result);
        }

        // POST api/<SignUpRequest>
        [HttpPost("SignUpRequest")]
        public async Task<IActionResult> CreateFarmerAccountAsync([FromBody] SignUpRequestDto SignUpRequestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAccount(SignUpRequestDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Created(Url.Action("GetById", "UserAccountsController", new { userId = ((UserAccountViewDetailsDto)result.Data).UserId }), result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/verify-email/userId={userId}&code={verificationCode}
        [HttpGet("verify-email/userId={userId}&code={verificationCode}")]
        public async Task<IActionResult> CreateFarmerAccountAsync(Guid userId, string verificationCode)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.VerifyEmail(userId, verificationCode);

            if (result.Status == Const.SUCCESS_VERIFY_OTP_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_VERIFY_OTP_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
