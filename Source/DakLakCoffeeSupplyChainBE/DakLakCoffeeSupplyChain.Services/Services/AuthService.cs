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
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
                    new Claim("avatar", user.ProfilePictureUrl ?? ""),
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
                var verifyUrl = $"{_config["Jwt:Issuer"]}/api/Auth/verify-email/userId={newUser.UserId}&code={verificationCode}";

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
                    _emailService.SendEmailAsync(newUser.Email, "Xác minh tài khoản", $"Click vào đường link này để xác minh tài khoản của bạn: <b>{verifyUrl}</b>");

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
                    var businessURL = $"{_config["Jwt:Issuer"]}/api/BusinessManagers/{businessManager.ManagerId}";
                    _emailService.SendEmailAsync("xuandang854@gmail.com", $"[DLC]Duyệt tài khoản doanh nghiệp {businessManager.CompanyName}", $"Click vào đường link này để xem và duyệt tài khoản của doanh nghiệp: <b>{businessURL}</b>");
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
                    var verifyUrlExisting = $"{_config["Jwt:Issuer"]}/api/Auth/verify-email/userId={user.UserId}&code={existingCode}";

                    // Gửi lại email xác minh với mã cũ
                    _emailService.SendEmailAsync(
                        user.Email,
                        "Xác minh tài khoản (gửi lại)",
                        $"Mã xác minh trước đó vẫn còn hiệu lực. Vui lòng click vào link sau để xác minh tài khoản của bạn: <b>{verifyUrlExisting}</b>"
                    );

                    return new ServiceResult(Const.SUCCESS_SEND_OTP_CODE    , "Đã gửi lại email xác minh (mã cũ vẫn còn hiệu lực).");
                }

                // 4. Nếu chưa có mã, tạo mã mới và lưu vào cache
                string newVerificationCode = GenerateVerificationCode(6);
                _cache.Set(cacheKey, newVerificationCode, TimeSpan.FromMinutes(30));

                var verifyUrlNew = $"{_config["Jwt:Issuer"]}/api/Auth/verify-email/userId={user.UserId}&code={newVerificationCode}";

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

            var resetUrl = $"{_config["AppSettings:FrontendUrl"]}/auth/reset-password?userId={user.UserId}&token={resetToken}"; // Đường dẫn reset mật khẩu

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #f97316, #ea580c); color: white; padding: 20px; border-radius: 10px; text-align: center;'>
                        <h1 style='margin: 0; font-size: 24px;'>🔄 Đặt lại mật khẩu</h1>
                        <p style='margin: 10px 0 0 0; opacity: 0.9;'>DakLak Coffee Supply Chain</p>
                    </div>
                    
                    <div style='background: white; padding: 30px; border-radius: 10px; margin-top: 20px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                        <h2 style='color: #374151; margin-bottom: 20px;'>Xin chào!</h2>
                        
                        <p style='color: #6b7280; line-height: 1.6; margin-bottom: 20px;'>
                            Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. 
                            Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.
                        </p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' 
                               style='background: linear-gradient(135deg, #f97316, #ea580c); 
                                      color: white; 
                                      padding: 15px 30px; 
                                      text-decoration: none; 
                                      border-radius: 8px; 
                                      font-weight: bold; 
                                      display: inline-block;
                                      box-shadow: 0 4px 6px rgba(249, 115, 22, 0.3);'>
                                🔐 Đặt lại mật khẩu
                            </a>
                        </div>
                        
                        <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0; color: #92400e; font-size: 14px;'>
                                <strong>⚠️ Lưu ý:</strong> Link này sẽ hết hạn sau 30 phút để đảm bảo an toàn.
                            </p>
                        </div>
                        
                        <p style='color: #6b7280; font-size: 14px; margin-top: 30px;'>
                            Nếu nút không hoạt động, bạn có thể copy và paste link sau vào trình duyệt:
                        </p>
                        <p style='background: #f3f4f6; padding: 10px; border-radius: 5px; word-break: break-all; font-size: 12px; color: #374151;'>
                            {resetUrl}
                        </p>
                    </div>
                    
                    <div style='text-align: center; margin-top: 20px; color: #9ca3af; font-size: 12px;'>
                        <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                        <p>© 2024 DakLak Coffee Supply Chain. All rights reserved.</p>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "🔄 Đặt lại mật khẩu - DakLak Coffee", emailBody);

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
