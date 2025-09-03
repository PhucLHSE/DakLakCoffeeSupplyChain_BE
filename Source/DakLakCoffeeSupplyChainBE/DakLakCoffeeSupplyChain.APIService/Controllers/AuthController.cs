using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMemoryCache _cache; // Thêm _cache vào

        public AuthController(IAuthService authService, IMemoryCache cache)
        {
            _authService = authService;
            _cache = cache;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);

            // Đăng nhập thành công, trả về token string
            if (result.Status == Const.SUCCESS_LOGIN_CODE && 
                result.Data is not null)
            {
                var tokenProp = result.Data.GetType().GetProperty("token");

                var tokenValue = tokenProp?.GetValue(result.Data)?.ToString();

                if (!string.IsNullOrEmpty(tokenValue))
                    return Ok(tokenValue); // 200
            }

            if (result.Status == Const.FAIL_READ_CODE)
                return Unauthorized(result.Message); // 401

            if (result.Status == Const.FAIL_VERIFY_OTP_CODE)
                return BadRequest(result.Message); // 400

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); // 404

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result.Message); // 500

            return StatusCode(500, result.Message); // fallback nếu không khớp
        }


        // POST api/<SignUpRequest>
        [HttpPost("SignUpRequest")]
        public async Task<IActionResult> CreateFarmerAccountAsync(
            [FromBody] SignUpRequestDto SignUpRequestDto)
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

        // POST api/register - New endpoint for mobile app
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Convert mobile app request to SignUpRequestDto
            var signUpRequest = new SignUpRequestDto
            {
                Email = request.Email,
                Password = request.Password,
                Name = request.FullName,
                RoleId = request.RoleId,
                Phone = request.Phone
            };

            var result = await _authService.RegisterAccount(signUpRequest);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(new { code = 200, message = "Đăng ký thành công", data = result.Data });

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(new { code = 409, message = result.Message });

            return StatusCode(500, new { code = 500, message = result.Message });
        }

        // GET api/verify-email/userId={userId}&code={verificationCode}
        [HttpGet("verify-email/userId={userId}&code={verificationCode}")]
        public async Task<IActionResult> VerifyEmailAsync(
            Guid userId, string verificationCode)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService
                .VerifyEmail(userId, verificationCode);

            if (result.Status == Const.SUCCESS_VERIFY_OTP_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_VERIFY_OTP_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/resend-verification-email
        [HttpPost("resend-verification-email")]
        public async Task<IActionResult> ResendVerificationEmail(
            [FromBody] ResendEmailVerificationRequestDto emailDto)
        {
            var result = await _authService
                .ResendVerificationEmail(emailDto);

            if (result.Status == Const.SUCCESS_SEND_OTP_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_VERIFY_OTP_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // Phương thức gửi mã OTP qua email
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService
                .ForgotPasswordAsync(request);

            if (result.Status == Const.SUCCESS_SEND_OTP_CODE)
                return Ok(new { success = true, message = result.Message });

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(new { success = false, message = result.Message });

            return StatusCode(500, new { success = false, message = result.Message });
        }

        // Phương thức để reset mật khẩu sau khi nhập mã OTP
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromQuery] Guid userId, 
            [FromQuery] string token, 
            [FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService
                .ResetPasswordAsync(userId, token, request);

            if (result.Status == Const.SUCCESS_RESET_PASSWORD_CODE)
                return Ok(new { success = true, message = result.Message });

            if (result.Status == Const.FAIL_RESET_PASSWORD_CODE)
                return BadRequest(new { success = false, message = result.Message });

            return StatusCode(500, new { success = false, message = result.Message });
        }

        // Phương thức xác minh mã OTP qua GET
        [HttpGet("reset-password/userId={userId}&token={token}")]
        public IActionResult ResetPasswordPage(
            Guid userId, string token)
        {
            // Kiểm tra mã reset có hợp lệ hay không
            var cacheKey = $"password-reset:{userId}";

            if (!_cache.TryGetValue(cacheKey, out string cachedToken) || cachedToken != token)
            {
                return NotFound(new { message = "Mã reset không hợp lệ hoặc đã hết hạn." });
            }

            // Nếu mã hợp lệ, trả về thông báo yêu cầu người dùng nhập mật khẩu mới
            return Ok(new { message = "Mã reset hợp lệ. Vui lòng nhập mật khẩu mới." });
        }
    }
}