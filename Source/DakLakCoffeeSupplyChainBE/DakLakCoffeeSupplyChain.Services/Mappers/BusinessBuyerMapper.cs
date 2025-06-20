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
        // Mapper BusinessManagerViewAllDto
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
    }
}
