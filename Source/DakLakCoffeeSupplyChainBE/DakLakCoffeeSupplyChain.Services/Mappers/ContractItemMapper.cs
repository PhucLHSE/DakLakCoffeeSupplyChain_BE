using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ContractItemMapper
    {
        // Mapper ContractItemViewDto
        public static ContractItemViewDto MapToContractItemViewDto(this ContractItem contractItem)
        {
            return new ContractItemViewDto
            {
                ContractItemId = contractItem.ContractItemId,
                ContractItemCode = contractItem.ContractItemCode ?? string.Empty,
                CoffeeTypeId = contractItem.CoffeeTypeId,
                CoffeeTypeName = contractItem.CoffeeType?.TypeName ?? string.Empty,
                Quantity = contractItem.Quantity,
                UnitPrice = contractItem.UnitPrice,
                DiscountAmount = contractItem.DiscountAmount,
                Note = contractItem.Note ?? string.Empty
            };
        }

        // Mapper ContractItemCreateDto -> ContractItem
        public static ContractItem MapToNewContractItem(this ContractItemCreateDto dto, string contractItemCode)
        {
            return new ContractItem
            {
                ContractItemId = Guid.NewGuid(),
                ContractItemCode = contractItemCode,
                ContractId = dto.ContractId,
                CoffeeTypeId = dto.CoffeeTypeId,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                DiscountAmount = dto.DiscountAmount ?? 0.0,
                Note = dto.Note ?? string.Empty,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Mapper ContractItemUpdateDto
        public static void MapToUpdateContractItem(this ContractItemUpdateDto dto, ContractItem contractItem)
        {
            contractItem.ContractId = dto.ContractId;
            contractItem.CoffeeTypeId = dto.CoffeeTypeId;
            contractItem.Quantity = dto.Quantity;
            contractItem.UnitPrice = dto.UnitPrice;
            contractItem.DiscountAmount = dto.DiscountAmount;
            contractItem.Note = dto.Note ?? string.Empty;
            contractItem.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
