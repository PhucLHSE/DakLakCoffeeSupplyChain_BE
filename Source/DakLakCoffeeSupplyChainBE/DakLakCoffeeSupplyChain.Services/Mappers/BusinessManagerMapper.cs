using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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

        // Mapper BusinessManagerCreateDto
        public static BusinessManager MapToNewBusinessManager(this BusinessManagerCreateDto dto, Guid userId, string managerCode)
        {
            return new BusinessManager
            {
                ManagerId = Guid.NewGuid(),
                UserId = userId,
                ManagerCode = managerCode,
                CompanyName = dto.CompanyName,
                Position = dto.Position,
                Department = dto.Department,
                CompanyAddress = dto.CompanyAddress,
                TaxId = dto.TaxId,
                Website = dto.Website,
                ContactEmail = dto.ContactEmail,
                BusinessLicenseUrl = dto.BusinessLicenseUrl,
                IsCompanyVerified = false,                    // Mặc định khi đăng ký là false
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Mapper BusinessManagerUpdateDto
        public static void MapToUpdateBusinessManager(this BusinessManagerUpdateDto dto, BusinessManager businessManager)
        {
            businessManager.CompanyName = dto.CompanyName;
            businessManager.Position = dto.Position;
            businessManager.Department = dto.Department;
            businessManager.CompanyAddress = dto.CompanyAddress;
            businessManager.TaxId = dto.TaxId;
            businessManager.Website = dto.Website;
            businessManager.ContactEmail = dto.ContactEmail;
            businessManager.BusinessLicenseUrl = dto.BusinessLicenseUrl;
            businessManager.IsCompanyVerified = dto.IsCompanyVerified;
            businessManager.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
