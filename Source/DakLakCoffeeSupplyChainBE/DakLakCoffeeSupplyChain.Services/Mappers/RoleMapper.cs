using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProductEnums;
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

        // Mapper RoleViewDetailsDto
        public static RoleViewDetailsDto MapToRoleViewDetailsDto(this Role role)
        {
            RoleStatus status = Enum.TryParse<RoleStatus>(role.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : RoleStatus.Inactive;

            return new RoleViewDetailsDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                Status = status,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };
        }
    }
}
