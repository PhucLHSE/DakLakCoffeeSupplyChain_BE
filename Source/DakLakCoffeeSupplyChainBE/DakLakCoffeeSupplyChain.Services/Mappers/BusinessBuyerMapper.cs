using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
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

        // Mapper BusinessBuyerViewDetailDto
        public static BusinessBuyerViewDetailDto MapToBusinessBuyerViewDetailDto(this BusinessBuyer businessBuyer)
        {
            return new BusinessBuyerViewDetailDto
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
    }
}
