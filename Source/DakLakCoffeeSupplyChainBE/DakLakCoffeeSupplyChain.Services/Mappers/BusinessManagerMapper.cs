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

        // Mapper BusinessManagerViewDetailsDto
        public static BusinessManagerViewDetailsDto MapToBusinessManagerViewDetailsDto(this BusinessManager manager)
        {
            return new BusinessManagerViewDetailsDto
            {
                ManagerId = manager.ManagerId,
                ManagerCode = manager.ManagerCode ?? string.Empty,
                CompanyName = manager.CompanyName ?? string.Empty,
                Position = manager.Position ?? string.Empty,
                Department = manager.Department ?? string.Empty,
                CompanyAddress = manager.CompanyAddress ?? string.Empty,
                TaxId = manager.TaxId ?? string.Empty,
                Website = manager.Website ?? string.Empty,
                ContactEmail = manager.ContactEmail ?? string.Empty,
                BusinessLicenseUrl = manager.BusinessLicenseUrl ?? string.Empty,
                IsCompanyVerified = manager.IsCompanyVerified,
                CreatedAt = manager.CreatedAt,
                UpdatedAt = manager.UpdatedAt,

                // User info
                FullName = manager.User?.Name ?? string.Empty,
                Email = manager.User?.Email ?? string.Empty,
                PhoneNumber = manager.User?.PhoneNumber ?? string.Empty
            };
        }
    }
}
