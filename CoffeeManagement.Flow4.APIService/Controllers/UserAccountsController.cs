using CoffeeManagement.Flow4.Repositories.Models;
using CoffeeManagement.Flow4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeManagement.Flow4.APIService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly UserAccountsService _userAccountsService;

        public UserAccountsController(IConfiguration config)
        {
            _config = config;
            _userAccountsService = new UserAccountsService();
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _userAccountsService.Authenticate(request.UserName, request.Password);


            if (user == null || user.Result == null)
                return Unauthorized();

            var token = GenerateJSONWebToken(user.Result);

            return Ok(token);
        }

        private string GenerateJSONWebToken(User userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                new Claim[]
                {
                    new(ClaimTypes.Name, userInfo.Email),
                    new(ClaimTypes.Role, userInfo.RoleId.ToString()),
                },
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenString;
        }

        public sealed record LoginRequest(string UserName, string Password);
    }
}
