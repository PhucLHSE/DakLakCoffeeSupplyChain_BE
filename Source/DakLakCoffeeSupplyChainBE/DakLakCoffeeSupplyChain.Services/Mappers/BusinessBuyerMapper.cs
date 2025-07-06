using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class BusinessBuyerMapper
    {
        // Mapper BusinessBuyerViewAllDto
        public static BusinessBuyerViewAllDto MapToBusinessBuyerViewAllDto(this BusinessBuyer businessBuyer)
        {
            return new BusinessBuyerViewAllDto
            {
                BuyerId = businessBuyer.BuyerId,
                BuyerCode = businessBuyer.BuyerCode ?? string.Empty,
                CompanyName = businessBuyer.CompanyName ?? string.Empty,
                ContactPerson = businessBuyer.ContactPerson ?? string.Empty,
                Position = businessBuyer.Position ?? string.Empty,
                CompanyAddress = businessBuyer.CompanyAddress ?? string.Empty,
                CreatedAt = businessBuyer.CreatedAt,
                CreatedByName = businessBuyer.CreatedByNavigation?.CompanyName ?? string.Empty
            };
        }

        // Mapper BusinessBuyerViewDetailsDto
        public static BusinessBuyerViewDetailsDto MapToBusinessBuyerViewDetailDto(this BusinessBuyer businessBuyer)
        {
            return new BusinessBuyerViewDetailsDto
            {
                BuyerId = businessBuyer.BuyerId,
                BuyerCode = businessBuyer.BuyerCode ?? string.Empty,
                CompanyName = businessBuyer.CompanyName ?? string.Empty,
                ContactPerson = businessBuyer.ContactPerson ?? string.Empty,
                Position = businessBuyer.Position ?? string.Empty,
                CompanyAddress = businessBuyer.CompanyAddress ?? string.Empty,
                TaxId = businessBuyer.TaxId ?? string.Empty,
                Email = businessBuyer.Email ?? string.Empty,
                Phone = businessBuyer.Phone ?? string.Empty,
                Website = businessBuyer.Website ?? string.Empty,
                CreatedAt = businessBuyer.CreatedAt,
                UpdatedAt = businessBuyer.UpdatedAt,
                CreatedByName = businessBuyer.CreatedByNavigation?.CompanyName ?? string.Empty
            };
        }

        // Mapper BusinessBuyerCreateDto
        public static BusinessBuyer MapToNewBusinessBuyer(this BusinessBuyerCreateDto dto, Guid managerId, string buyerCode)
        {
            return new BusinessBuyer
            {
                BuyerId = Guid.NewGuid(),
                BuyerCode = buyerCode,
                CompanyName = dto.CompanyName,
                ContactPerson = dto.ContactPerson,
                Position = dto.Position,
                CompanyAddress = dto.CompanyAddress,
                TaxId = dto.TaxId,
                Email = dto.Email,
                Phone = dto.PhoneNumber,
                Website = dto.Website,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                CreatedBy = managerId
            };
        }

        // Mapper BusinessBuyerUpdateDto
        public static void MapToUpdateBusinessBuyer(this BusinessBuyerUpdateDto dto, BusinessBuyer businessBuyer)
        {
            businessBuyer.CompanyName = dto.CompanyName;
            businessBuyer.ContactPerson = dto.ContactPerson;
            businessBuyer.Position = dto.Position;
            businessBuyer.CompanyAddress = dto.CompanyAddress;
            businessBuyer.TaxId = dto.TaxId;
            businessBuyer.Email = dto.Email;
            businessBuyer.Phone = dto.PhoneNumber;
            businessBuyer.Website = dto.Website;
            businessBuyer.UpdatedAt = DateHelper.NowVietnamTime(); // Cập nhật thời gian chỉnh sửa
        }
    }
}
