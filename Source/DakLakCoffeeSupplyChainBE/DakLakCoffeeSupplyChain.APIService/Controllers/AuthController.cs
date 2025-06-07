using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (result.Status != 200)
                return StatusCode(result.Status, new
                {
                    status = result.Status,
                    message = result.Message,
                    token = result.Data
                });

            return StatusCode(result.Status, new
            {
                status = result.Status,
                message = result.Message,
                token = result.Data
            });
        }
    }
}