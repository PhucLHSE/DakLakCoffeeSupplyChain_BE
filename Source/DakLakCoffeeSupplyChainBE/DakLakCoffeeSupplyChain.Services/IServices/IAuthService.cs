using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IAuthService
    {
        Task<IServiceResult> LoginAsync(LoginRequestDto request);
        Task<IServiceResult> RegisterAccount(SignUpRequestDto request);
        Task<IServiceResult> VerifyEmail(Guid userId, string code);
        Task<IServiceResult> ResendVerificationEmail(ResendEmailVerificationRequestDto emailDto);
        Task<IServiceResult> ForgotPasswordAsync(ForgotPasswordRequestDto request); // Thêm phương thức yêu cầu đặt lại mật khẩu
        Task<IServiceResult> ResetPasswordAsync(Guid userId, string token, ResetPasswordRequestDto request); // Thêm phương thức thay đổi mật khẩu
    }
}
