using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ContractMapper
    {
        // Mapper ContractViewAllDto
        public static ContractViewAllDto MapToContractViewAllDto(
            this Contract contract)
        {
            // Parse Status string to enum
            ContractStatus status = Enum.TryParse<ContractStatus>
                (contract.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractStatus.NotStarted;

            return new ContractViewAllDto
            {
                ContractId = contract.ContractId,
                ContractCode = contract.ContractCode,
                ContractNumber = contract.ContractNumber,
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

        // Mapper ContractViewDetailsDto
        public static ContractViewDetailsDto MapToContractViewDetailDto(
            this Contract contract)
        {
            // Parse Status string to enum
            ContractStatus status = Enum.TryParse<ContractStatus>
                (contract.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractStatus.NotStarted;

            SettlementStatus settlement = Enum.TryParse(contract.SettlementStatus, true, out SettlementStatus parsedSettle)
                ? parsedSettle
                : SettlementStatus.None;

            // Parse SettlementFilesJson -> List<string>
            var settlementFiles = new List<string>();

            if (!string.IsNullOrWhiteSpace(contract.SettlementFilesJson))
            {
                try
                {
                    // Ưu tiên mảng string ["url1","url2"]; nếu bạn lưu object thì có thể đổi sang List<YourFileDto>
                    settlementFiles = JsonSerializer.Deserialize<List<string>>(contract.SettlementFilesJson)
                                      ?? new List<string>();
                }
                catch
                {
                    // Không chặn flow nếu JSON xấu
                }
            }

            return new ContractViewDetailsDto
            {
                ContractId = contract.ContractId,
                ContractCode = contract.ContractCode ?? string.Empty,
                ContractNumber = contract.ContractNumber ?? string.Empty,
                ContractTitle = contract.ContractTitle ?? string.Empty,
                ContractFileUrl = contract.ContractFileUrl ?? string.Empty,

                SellerName = contract.Seller?.User?.Name ?? "N/A",
                BuyerId = contract.BuyerId,
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

                ContractType = contract.ContractType ?? string.Empty,
                ParentContractId = contract.ParentContractId,
                ParentContractCode = contract.ParentContract?.ContractCode ?? string.Empty,
                PaymentRounds = contract.PaymentRounds,
                SettlementStatus = settlement,
                SettledAt = contract.SettledAt,
                SettlementFileUrl = contract.SettlementFileUrl ?? string.Empty,
                SettlementFiles = settlementFiles,
                SettlementNote = contract.SettlementNote ?? string.Empty,

                ContractItems = contract.ContractItems
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
        public static Contract MapToNewContract(
            this ContractCreateDto dto, 
            Guid sellerId,
            string contractCode)
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

        // Mapper ContractUpdateDto
        public static void MapToUpdateContract(
            this Contract contract,
            ContractUpdateDto dto)
        {
            // Cập nhật thông tin contract chính
            contract.BuyerId = dto.BuyerId;
            contract.ContractNumber = dto.ContractNumber;
            contract.ContractTitle = dto.ContractTitle;
            contract.ContractFileUrl = dto.ContractFileUrl;
            contract.DeliveryRounds = dto.DeliveryRounds;
            contract.TotalQuantity = dto.TotalQuantity;
            contract.TotalValue = dto.TotalValue;
            contract.StartDate = dto.StartDate ?? default;
            contract.EndDate = dto.EndDate ?? default;
            contract.SignedAt = dto.SignedAt;
            contract.Status = dto.Status.ToString();
            contract.CancelReason = dto.CancelReason;
            contract.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
