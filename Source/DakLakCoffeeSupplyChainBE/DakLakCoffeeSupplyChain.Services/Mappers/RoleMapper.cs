using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.RoleEnums;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class RoleMapper
    {
        // Mapper RoleViewAllDto
        public static RoleViewAllDto MapToRoleViewAllDto(this Role role)
        {
            // Parse Status string to enum
            RoleStatus status = Enum.TryParse<RoleStatus>(role.Status, ignoreCase: true, out var parsedStatus)
                     ? parsedStatus
                     : RoleStatus.Inactive;

            return new RoleViewAllDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Status = status
            };
        }
    }
}
