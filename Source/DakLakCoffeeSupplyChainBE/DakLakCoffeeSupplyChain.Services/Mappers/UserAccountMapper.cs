using DakLakCoffeeSupplyChain.Common.DTOs.AuthDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class UserAccountMapper
    {
        // Mapper UserAccountViewAllDto
        public static UserAccountViewAllDto MapToUserAccountViewAllDto(this UserAccount userAccount)
        {
            // Parse Status string to enum
            UserAccountStatus status = Enum.TryParse<UserAccountStatus>(userAccount.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : UserAccountStatus.Unknown;

            return new UserAccountViewAllDto
            {
                UserId = userAccount.UserId,
                UserCode = userAccount.UserCode ?? string.Empty,
                Name = userAccount.Name ?? string.Empty,
                Email = userAccount.Email ?? string.Empty,
                PhoneNumber = userAccount.PhoneNumber ?? string.Empty,
                RoleName = userAccount.Role?.RoleName ?? string.Empty,
                LastLogin = userAccount.LastLogin,
                RegistrationDate = userAccount.RegistrationDate,
                Status = status
            };
        }

        // Mapper UserAccountViewDetailsDto
        public static UserAccountViewDetailsDto MapToUserAccountViewDetailsDto(this UserAccount userAccount)
        {
            // Parse Gender string to enum
            Gender gender = Enum.TryParse<Gender>(userAccount.Gender, ignoreCase: true, out var parsedGender)
                ? parsedGender
                : Gender.Unknown;

            // Parse Status string to enum
            UserAccountStatus status = Enum.TryParse<UserAccountStatus>(userAccount.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : UserAccountStatus.Unknown;

            return new UserAccountViewDetailsDto
            {
                UserId = userAccount.UserId,
                UserCode = userAccount.UserCode ?? string.Empty,
                Email = userAccount.Email ?? string.Empty,
                PhoneNumber = userAccount.PhoneNumber ?? string.Empty,
                Name = userAccount.Name ?? string.Empty,
                Gender = gender,
                DateOfBirth = userAccount.DateOfBirth,
                Address = userAccount.Address ?? string.Empty,
                ProfilePictureUrl = userAccount.ProfilePictureUrl ?? string.Empty,
                EmailVerified = userAccount.EmailVerified,
                IsVerified = userAccount.IsVerified,
                LoginType = userAccount.LoginType ?? string.Empty,
                Status = status,
                RoleName = userAccount.Role?.RoleName ?? string.Empty,
                RegistrationDate = userAccount.RegistrationDate,
                LastLogin = userAccount.LastLogin,
                UpdatedAt = userAccount.UpdatedAt
            };
        }

        // Mapper UserAccountCreateDto
        public static UserAccount MapToNewUserAccount(this UserAccountCreateDto dto, string passwordHash, string userCode, int RoleId)
        {
            return new UserAccount
            {
                UserId = Guid.NewGuid(),
                UserCode = userCode, // Được sinh từ Service
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Name = dto.Name,
                Gender = dto.Gender.ToString(), // enum → string
                DateOfBirth = dto.DateOfBirth,
                Address = dto.Address,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                PasswordHash = passwordHash,
                LoginType = dto.LoginType.ToString(), // enum → string
                Status = dto.Status.ToString(),       // enum → string
                RoleId = RoleId,
                RegistrationDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailVerified = true,         // Admin tạo thì email được xem là xác thực
                IsVerified = true,            // Có thể xem như đã duyệt
                VerificationCode = null,      // Không cần tạo mã xác minh
            };
        }

        // Mapper UserAccountUpdateDto
        public static void MapToUserAccountUpdateDto(this UserAccountUpdateDto dto, UserAccount userAccount, int roleId)
        {
            userAccount.Email = dto.Email;
            userAccount.PhoneNumber = dto.PhoneNumber;
            userAccount.Name = dto.Name;
            userAccount.Gender = dto.Gender.ToString(); // enum → string
            userAccount.DateOfBirth = dto.DateOfBirth;
            userAccount.Address = dto.Address;
            userAccount.ProfilePictureUrl = dto.ProfilePictureUrl;
            userAccount.Status = dto.Status.ToString(); // enum → string
            userAccount.RoleId = roleId;
            userAccount.UpdatedAt = DateTime.UtcNow;
        }  
    }
}