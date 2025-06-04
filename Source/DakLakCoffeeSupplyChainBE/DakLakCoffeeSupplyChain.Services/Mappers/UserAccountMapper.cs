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

                // Parse string to enum, fallback to Unknown
                Status = Enum.TryParse<UserAccountStatus>(entity.Status, true, out var parsedStatus)
                    ? parsedStatus
                    : UserAccountStatus.Unknown
            };
        }
    }
}
