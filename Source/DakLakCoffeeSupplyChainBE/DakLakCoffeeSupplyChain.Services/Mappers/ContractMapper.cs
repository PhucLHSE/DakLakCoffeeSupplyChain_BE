using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ContractMapper
    {
        // Mapper ContractViewAllDto
        public static ContractViewAllDto MapToContractViewAllDto(this Contract contract)
        {
            // Parse Status string to enum
            ContractStatus status = Enum.TryParse<ContractStatus>(contract.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractStatus.NotStarted;

            return new ContractViewAllDto
            {
                ContractId = contract.ContractId,
                ContractCode = contract.ContractCode,
                ContractTitle = contract.ContractTitle,
                SellerName = contract.Seller?.User?.Name ?? "N/A",
                BuyerName = contract.Buyer?.CompanyName ?? "N/A",
                DeliveryRounds = contract.DeliveryRounds,
                TotalQuantity = contract.TotalQuantity,
                TotalValue = contract.TotalValue,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = status
            };
        }

        // Mapper ContractViewDetailDto
        public static ContractViewDetailDto MapToContractViewDetailDto(this Contract contract)
        {
            // Parse Status string to enum
            ContractStatus status = Enum.TryParse<ContractStatus>(contract.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractStatus.NotStarted;

            return new ContractViewDetailDto
            {
                ContractId = contract.ContractId,
                ContractCode = contract.ContractCode ?? string.Empty,
                ContractNumber = contract.ContractNumber ?? string.Empty,
                ContractTitle = contract.ContractTitle ?? string.Empty,
                ContractFileUrl = contract.ContractFileUrl ?? string.Empty,
                SellerName = contract.Seller?.User?.Name ?? "N/A",
                BuyerName = contract.Buyer?.CompanyName ?? "N/A",
                DeliveryRounds = contract.DeliveryRounds,
                TotalQuantity = contract.TotalQuantity,
                TotalValue = contract.TotalValue,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                SignedAt = contract.SignedAt,
                Status = status,
                CancelReason = contract.CancelReason ?? string.Empty,
                CreatedAt = contract.CreatedAt,
                UpdatedAt = contract.UpdatedAt,
                Items = contract.ContractItems
                    .Where(item => !item.IsDeleted)
                    .Select(item => new ContractItemViewDto
                    {
                        ContractItemId = item.ContractItemId,
                        ContractItemCode = item.ContractItemCode ?? string.Empty,
                        CoffeeTypeId = item.CoffeeTypeId,
                        CoffeeTypeName = item.CoffeeType?.TypeName ?? "Unknown",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        Note = item.Note ?? string.Empty
                    })
                    .ToList()
            };
        }

        // Mapper ContractCreateDto
        public static Contract MapToNewContract(this ContractCreateDto dto, Guid sellerId, string contractCode)
        {
            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                ContractCode = contractCode,
                ContractNumber = dto.ContractNumber,
                ContractTitle = dto.ContractTitle,
                ContractFileUrl = dto.ContractFileUrl,
                BuyerId = dto.BuyerId,
                SellerId = sellerId,
                DeliveryRounds = dto.DeliveryRounds,
                TotalQuantity = dto.TotalQuantity,
                TotalValue = dto.TotalValue,
                StartDate = dto.StartDate ?? default,
                EndDate = dto.EndDate ?? default,
                SignedAt = dto.SignedAt,
                Status = dto.Status.ToString(), // enum → string
                CancelReason = dto.CancelReason,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false,

                ContractItems = dto.ContractItems.Select((itemDto, index) => new ContractItem
                {
                    ContractItemId = Guid.NewGuid(),
                    ContractItemCode = $"CTI-{index + 1:D3}-{contractCode}",
                    CoffeeTypeId = itemDto.CoffeeTypeId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountAmount = itemDto.DiscountAmount,
                    Note = itemDto.Note,
                    IsDeleted = false,
                    CreatedAt = DateHelper.NowVietnamTime(),
                    UpdatedAt = DateHelper.NowVietnamTime()
                }).ToList()
            };

            return contract;
        }
    }
}
