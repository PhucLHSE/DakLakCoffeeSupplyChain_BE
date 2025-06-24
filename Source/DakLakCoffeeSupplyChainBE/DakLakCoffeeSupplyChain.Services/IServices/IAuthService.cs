using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IAuthService
    {
        Task<IServiceResult> LoginAsync(LoginRequestDto request);
        Task<IServiceResult> RegisterAccount(SignUpRequestDto request);
        Task<IServiceResult> VerifyEmail(Guid userId, string code);
    }
}
