using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers.Security;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class AuthService(IUnitOfWork unitOfWork, IConfiguration config, IMemoryCache cache, IPasswordHasher passwordHasher, ICodeGenerator codeGenerator, IEmailService emailService) : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IConfiguration _config = config;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ICodeGenerator _codeGenerator = codeGenerator;
        private readonly IEmailService _emailService = emailService;
        private readonly IMemoryCache _cache = cache;

        public async Task<IServiceResult> LoginAsync(LoginRequestDto request)
        {
            // 1. Tìm user theo email
            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByEmailAsync(request.Email);
            if (user == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Email hoặc mật khẩu không đúng.");

            // 2. So sánh mật khẩu bằng hasher
            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
                return new ServiceResult(Const.FAIL_READ_CODE, "Email hoặc mật khẩu không đúng.");

            // 3. Kiểm tra xác minh isVerify
            if (!(user.IsVerified ?? false))
                return new ServiceResult(Const.FAIL_READ_CODE, "Tài khoản chưa được duyệt.");

            // 4. Kiểm tra xác minh email
            if (!(user.EmailVerified ?? false))
                return new ServiceResult(Const.FAIL_READ_CODE, "Tài khoản chưa xác minh email.");

            // 5. Kiểm tra duyệt
            if (user.Status?.ToLower() != "active")
                return new ServiceResult(Const.FAIL_READ_CODE, "Tài khoản chưa được duyệt hoặc đã bị khóa.");

            // 6. Tạo token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim("name", user.Name),
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

        public async Task<IServiceResult> RegisterAccount(SignUpRequestDto request)
        {
            try
            {
                // Kiểm tra email đã tồn tại chưa
                var emailExists = await _unitOfWork.UserAccountRepository.GetUserAccountByEmailAsync(request.Email);

                if (emailExists != null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Email đã được đăng ký."
                    );
                }

                // Kiểm tra phone đã tồn tại chưa
                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    var phoneExists = await _unitOfWork.UserAccountRepository.GetUserAccountByPhoneAsync(request.Phone);

                    if (phoneExists != null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Số điện thoại đã được đăng ký."
                        );
                    }
                }

                // Generate password hash và user code
                string passwordHash = _passwordHasher.Hash(request.Password); // hoặc bất kỳ method nào của bạn
                string userCode = await _codeGenerator.GenerateUserCodeAsync(); // ví dụ: "USR-YYYY-####" hoặc Guid, tuỳ bạn

                // Map DTO to Entity
                var newUser = request.MapToNewAccount(passwordHash, userCode);

                //Tạo mã verify email, lưu trong ram của server, thời hạn 30 phút, nếu như hệ thống bị tắt thì sẽ xóa hết trong ram
                string verificationCode = GenerateVerificationCode(6);
                _cache.Set($"email-verify:{newUser.UserId}", verificationCode, TimeSpan.FromMinutes(30));
                var verifyUrl = $"https://localhost:7163/api/Auth/verify-email/userId={newUser.UserId}&code={verificationCode}";

                // Tạo người dùng ở repository
                await _unitOfWork.UserAccountRepository.CreateAsync(newUser);

                // Phân role
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(request.RoleId);
                if (role.RoleName == "Farmer")
                {
                    string farmerCode = await _codeGenerator.GenerateFarmerCodeAsync();
                    Farmer newFarmer = new()
                    {
                        FarmerId = Guid.NewGuid(),
                        FarmerCode = farmerCode,
                        UserId = newUser.UserId
                    };
                    await _unitOfWork.FarmerRepository.CreateAsync(newFarmer);
                }
                else if (role.RoleName == "BusinessManager")
                {
                    string managerCode = await _codeGenerator.GenerateManagerCodeAsync();
                    BusinessManager newBusinessManager = new()
                    {
                        ManagerId = Guid.NewGuid(),
                        ManagerCode = managerCode,
                        CompanyName = request.CompanyName,
                        TaxId = request.TaxId,
                        BusinessLicenseUrl = request.BusinessLicenseURl,
                        UserId = newUser.UserId
                    };
                    await _unitOfWork.BusinessManagerRepository.CreateAsync(newBusinessManager);
                }
                else
                    return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            "Role không hợp lệ"
                        );


                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = newUser.MapToUserAccountViewDetailsDto();

                    // Gửi email xác minh
                    await _emailService.SendEmailAsync(newUser.Email, "Xác minh tài khoản", $"Click vào đường link này để xác minh tài khoản của bạn: <b>{verifyUrl}</b>");

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public static string GenerateVerificationCode(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string([.. Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)])]);
        }

        public async Task<IServiceResult> VerifyEmail(Guid userId, string code)
        {
            try
            {
                // 1. Tìm người dùng
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                if (user == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA_MSG);

                // 2. Kiểm tra đã xác minh chưa
                if (user.IsVerified == true)
                    return new ServiceResult(Const.FAIL_VERIFY_OTP_CODE, "Tài khoản đã được xác minh trước đó.");

                // 3. Lấy mã xác minh từ cache
                var cacheKey = $"email-verify:{userId}";
                if (!_cache.TryGetValue(cacheKey, out string cachedCode))
                    return new ServiceResult(Const.FAIL_VERIFY_OTP_CODE, "Mã xác minh đã hết hạn hoặc không tồn tại.");

                // 4. So sánh mã
                if (cachedCode != code)
                    return new ServiceResult(Const.FAIL_VERIFY_OTP_CODE, "Mã xác minh không hợp lệ.");

                // 5. Cập nhật trạng thái người dùng
                user.IsVerified = true;
                user.EmailVerified = true;
                user.Status = "active";
                await _unitOfWork.UserAccountRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // 6. Xoá mã trong cache
                _cache.Remove(cacheKey);

                // 7. Thông báo cho admin duyệt nếu là tài khoản của business manager
                //Tạm thời hard code email của admin và url
                //Tạm thời admin vẫn chưa duyệt được và cũng chưa xem được admin vì bị 401 chưa đăng nhập
                //Đường link trong tương lai phải trả về trang chủ đăng nhập của FE, tạm thời chưa có FE
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: p => p.UserId == userId,
                    include: p => p.Include(p => p.User).ThenInclude(p => p.Role),
                    asNoTracking: true
                    );

                if (businessManager != null && businessManager.User.Role.RoleName == "BusinessManager")
                {
                    var businessURL = $"https://localhost:7163/api/BusinessManagers/{businessManager.ManagerId}";
                    await _emailService.SendEmailAsync("xuandang854@gmail.com", $"[DLC]Duyệt tài khoản doanh nghiệp {businessManager.CompanyName}", $"Click vào đường link này để xem và duyệt tài khoản của doanh nghiệp: <b>{businessURL}</b>");
                }

                return new ServiceResult(Const.SUCCESS_VERIFY_OTP_CODE, "Xác minh email thành công.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        public async Task<IServiceResult> ResendVerificationEmail(ResendEmailVerificationRequestDto emailDto)
        {
            try
            {
                // 1. Lấy thông tin người dùng
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.Email == emailDto.Email,
                    asNoTracking: true
                    );
                if (user == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Tài khoản không tồn tại.");

                // 2. Kiểm tra đã xác minh chưa
                if (user.IsVerified == true)
                    return new ServiceResult(Const.FAIL_VERIFY_OTP_CODE, "Tài khoản đã được xác minh trước đó.");

                // 3. Kiểm tra xem đã có mã xác minh trong cache chưa
                var cacheKey = $"email-verify:{user.UserId}";
                if (_cache.TryGetValue(cacheKey, out string existingCode))
                {
                    var verifyUrlExisting = $"https://localhost:7163/api/Auth/verify-email/userId={user.UserId}&code={existingCode}";

                    // Gửi lại email xác minh với mã cũ
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Xác minh tài khoản (gửi lại)",
                        $"Mã xác minh trước đó vẫn còn hiệu lực. Vui lòng click vào link sau để xác minh tài khoản của bạn: <b>{verifyUrlExisting}</b>"
                    );

                    return new ServiceResult(Const.SUCCESS_SEND_OTP_CODE    , "Đã gửi lại email xác minh (mã cũ vẫn còn hiệu lực).");
                }

                // 4. Nếu chưa có mã, tạo mã mới và lưu vào cache
                string newVerificationCode = GenerateVerificationCode(6);
                _cache.Set(cacheKey, newVerificationCode, TimeSpan.FromMinutes(30));

                var verifyUrlNew = $"https://localhost:7163/api/Auth/verify-email/userId={user.UserId}&code={newVerificationCode}";

                // 5. Gửi email xác minh mới
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Xác minh tài khoản (mã mới)",
                    $"Click vào đường link sau để xác minh tài khoản của bạn: <b>{verifyUrlNew}</b>"
                );

                return new ServiceResult(Const.SUCCESS_SEND_OTP_CODE, "Đã tạo và gửi email xác minh mới.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.ToString());
            }
        }

        // Phương thức gửi email với mã OTP reset mật khẩu
        public async Task<IServiceResult> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _unitOfWork.UserAccountRepository.GetUserAccountByEmailAsync(request.Email);
            if (user == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Email không tồn tại.");

            var resetToken = GenerateResetToken(); // Tạo mã reset
            _cache.Set($"password-reset:{user.UserId}", resetToken, TimeSpan.FromMinutes(30)); // Lưu mã trong cache

            var resetUrl = $"https://localhost:7163/api/Auth/reset-password/userId={user.UserId}&token={resetToken}"; // Đường dẫn reset mật khẩu

            await _emailService.SendEmailAsync(user.Email, "Reset mật khẩu", $"Click vào đường link này để thay đổi mật khẩu của bạn: <b>{resetUrl}</b>");

            return new ServiceResult(Const.SUCCESS_SEND_OTP_CODE, "Mã OTP đã được gửi qua email.");
        }

        // Phương thức kiểm tra mã token và thay đổi mật khẩu
        public async Task<IServiceResult> ResetPasswordAsync(Guid userId, string token, ResetPasswordRequestDto request)
        {
            var cacheKey = $"password-reset:{userId}";
            if (!_cache.TryGetValue(cacheKey, out string cachedToken) || cachedToken != token)
            {
                return new ServiceResult(Const.FAIL_RESET_PASSWORD_CODE, "Mã reset không hợp lệ hoặc đã hết hạn.");
            }

            // Cập nhật mật khẩu mới
            var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
            if (user == null)
                return new ServiceResult(Const.FAIL_READ_CODE, "Người dùng không tồn tại.");

            string passwordHash = _passwordHasher.Hash(request.NewPassword);
            user.PasswordHash = passwordHash;
            await _unitOfWork.UserAccountRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Xóa mã token sau khi đổi mật khẩu
            _cache.Remove(cacheKey);

            return new ServiceResult(Const.SUCCESS_RESET_PASSWORD_CODE, "Mật khẩu đã được thay đổi.");
        }


        // Phương thức tạo mã reset ngẫu nhiên
        public static string GenerateResetToken(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
