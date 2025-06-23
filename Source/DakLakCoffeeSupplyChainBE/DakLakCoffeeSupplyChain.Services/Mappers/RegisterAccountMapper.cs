using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs.ProcurementPlanViews;
using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class RegisterAccountMapper
    {
        //Mapper đăng ký tài khoản cho farmer
        public static UserAccount MapToNewAccount(this SignUpRequestDto dto, string passwordHash, string userCode)
        {
            return new UserAccount
            {
                UserId = Guid.NewGuid(),
                UserCode = userCode,
                Email = dto.Email,
                PhoneNumber = dto.Phone,
                Name = dto.Name,
                PasswordHash = passwordHash,
                Status = UserAccountStatus.PendingApproval.ToString(),
                RoleId = dto.RoleId,
                RegistrationDate = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                EmailVerified = false,         //Duyệt OTP
            };
        }
    }
}
