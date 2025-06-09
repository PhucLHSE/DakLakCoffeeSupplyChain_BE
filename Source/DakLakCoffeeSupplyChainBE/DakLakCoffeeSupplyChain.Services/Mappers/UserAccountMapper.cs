using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.UserAccountEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class UserAccountMapper
    {
        // Mapper UserAccountViewAllDto
        public static UserAccountViewAllDto MapToUserAccountViewAllDto(this UserAccount entity)
        {
            // Parse Status string to enum
            UserAccountStatus status = Enum.TryParse<UserAccountStatus>(entity.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : UserAccountStatus.Unknown;

            return new UserAccountViewAllDto
            {
                UserId = entity.UserId,
                UserCode = entity.UserCode ?? string.Empty,
                Name = entity.Name ?? string.Empty,
                Email = entity.Email ?? string.Empty,
                PhoneNumber = entity.PhoneNumber ?? string.Empty,
                RoleName = entity.Role?.RoleName ?? string.Empty,
                LastLogin = entity.LastLogin,
                RegistrationDate = entity.RegistrationDate,
                Status = status
            };
        }

        // Mapper UserAccountViewDetailsDto
        public static UserAccountViewDetailsDto MapToUserAccountViewDetailsDto(this UserAccount entity)
        {
            // Parse Gender string to enum
            Gender gender = Enum.TryParse<Gender>(entity.Gender, ignoreCase: true, out var parsedGender)
                ? parsedGender
                : Gender.Unknown;

            // Parse Status string to enum
            UserAccountStatus status = Enum.TryParse<UserAccountStatus>(entity.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : UserAccountStatus.Unknown;

            return new UserAccountViewDetailsDto
            {
                UserId = entity.UserId,
                UserCode = entity.UserCode ?? string.Empty,
                Email = entity.Email ?? string.Empty,
                PhoneNumber = entity.PhoneNumber ?? string.Empty,
                Name = entity.Name ?? string.Empty,
                Gender = gender,
                DateOfBirth = entity.DateOfBirth,
                Address = entity.Address ?? string.Empty,
                ProfilePictureUrl = entity.ProfilePictureUrl ?? string.Empty,
                EmailVerified = entity.EmailVerified,
                IsVerified = entity.IsVerified,
                LoginType = entity.LoginType ?? string.Empty,
                Status = status,
                RoleName = entity.Role?.RoleName ?? string.Empty,
                RegistrationDate = entity.RegistrationDate,
                LastLogin = entity.LastLogin,
                UpdatedAt = entity.UpdatedAt
            };
        }

        // Mapper UserAccountCreateDto
        public static UserAccount MapToUserAccountCreateDto(this UserAccountCreateDto dto, string passwordHash, string userCode, int RoleId)
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
    }
}
