using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class BusinessManagerMapper
    {
        // Mapper BusinessManagerViewAllDto
        public static BusinessManagerViewAllDto MapToBusinessManagerViewAllDto(this BusinessManager manager)
        {
            return new BusinessManagerViewAllDto
            {
                ManagerId = manager.ManagerId,
                ManagerCode = manager.ManagerCode,
                CompanyName = manager.CompanyName,
                Position = manager.Position,
                Department = manager.Department,
                IsCompanyVerified = manager.IsCompanyVerified
            };
        }
    }
}
