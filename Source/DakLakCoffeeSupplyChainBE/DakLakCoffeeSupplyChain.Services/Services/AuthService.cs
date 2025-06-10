using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserAccountRepository _userRepo;
        private readonly IConfiguration _config;

        public AuthService(IUserAccountRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IServiceResult> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepo.GetUserByCredentialsAsync(request.Email, request.Password);
            if (user == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Email hoặc mật khẩu không đúng.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
                }),
                Expires = DateTime.UtcNow.AddHours(3),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new ServiceResult(Const.SUCCESS_LOGIN_CODE, "Đăng nhập thành công", new { token = tokenString });
        }
    }
}
