using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Common.Enum.ContractDeliveryBatchEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ContractDeliveryBatchMapper
    {
        // Mapper từ entity sang ViewAllDto
        public static ContractDeliveryBatchViewAllDto MapToContractDeliveryBatchViewAllDto(
            this ContractDeliveryBatch contractDeliveryBatch)
        {
            // Parse Status string từ entity sang enum
            ContractDeliveryBatchStatus status = Enum.TryParse<ContractDeliveryBatchStatus>
                (contractDeliveryBatch.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractDeliveryBatchStatus.Planned;

            return new ContractDeliveryBatchViewAllDto
            {
                DeliveryBatchId = contractDeliveryBatch.DeliveryBatchId,
                DeliveryBatchCode = contractDeliveryBatch.DeliveryBatchCode ?? string.Empty,
                ContractId = contractDeliveryBatch.ContractId,
                ContractNumber = contractDeliveryBatch.Contract.ContractNumber ?? string.Empty,
                DeliveryRound = contractDeliveryBatch.DeliveryRound,
                ExpectedDeliveryDate = contractDeliveryBatch.ExpectedDeliveryDate,
                TotalPlannedQuantity = contractDeliveryBatch.TotalPlannedQuantity,
                Status = status,
                CreatedAt = contractDeliveryBatch.CreatedAt,
                UpdatedAt = contractDeliveryBatch.UpdatedAt
            };
        }

        // Mapper ContractDeliveryBatchViewDetailsDto
        public static ContractDeliveryBatchViewDetailsDto MapToContractDeliveryBatchViewDetailDto(
            this ContractDeliveryBatch contractDeliveryBatch)
        {
            // Parse Status string từ entity sang enum
            ContractDeliveryBatchStatus status = Enum.TryParse<ContractDeliveryBatchStatus>
                (contractDeliveryBatch.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractDeliveryBatchStatus.Planned;

            return new ContractDeliveryBatchViewDetailsDto
            {
                DeliveryBatchId = contractDeliveryBatch.DeliveryBatchId,
                DeliveryBatchCode = contractDeliveryBatch.DeliveryBatchCode ?? string.Empty,
                ContractId = contractDeliveryBatch.ContractId,
                ContractNumber = contractDeliveryBatch.Contract?.ContractNumber ?? string.Empty,
                ContractTitle = contractDeliveryBatch.Contract?.ContractTitle ?? string.Empty,
                DeliveryRound = contractDeliveryBatch.DeliveryRound,
                ExpectedDeliveryDate = contractDeliveryBatch.ExpectedDeliveryDate,
                TotalPlannedQuantity = contractDeliveryBatch.TotalPlannedQuantity,
                Status = status,
                CreatedAt = contractDeliveryBatch.CreatedAt,
                UpdatedAt = contractDeliveryBatch.UpdatedAt,
                ContractDeliveryItems = contractDeliveryBatch.ContractDeliveryItems?
                    .Where(item => !item.IsDeleted)
                    .Select(item => new ContractDeliveryItemViewDto
                    {
                        DeliveryItemId = item.DeliveryItemId,
                        DeliveryItemCode = item.DeliveryItemCode ?? string.Empty,
                        ContractItemId = item.ContractItemId,
                        CoffeeTypeName = item.ContractItem?.CoffeeType?.TypeName ?? string.Empty,
                        PlannedQuantity = item.PlannedQuantity,
                        FulfilledQuantity = item.FulfilledQuantity,
                        Note = item.Note ?? string.Empty
                    })
                    .ToList() ?? new List<ContractDeliveryItemViewDto>()
            };
        }

        // Mapper ContractDeliveryBatchCreateDto -> ContractDeliveryBatch
        public static ContractDeliveryBatch MapToNewContractDeliveryBatch(
            this ContractDeliveryBatchCreateDto dto,
            string deliveryBatchCode)
        {
            var batchId = Guid.NewGuid();

            var deliveryBatch = new ContractDeliveryBatch
            {
                DeliveryBatchId = batchId,
                DeliveryBatchCode = deliveryBatchCode,
                ContractId = dto.ContractId,
                DeliveryRound = dto.DeliveryRound,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                TotalPlannedQuantity = dto.TotalPlannedQuantity,
                Status = dto.Status.ToString(), // enum to string
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false,

                ContractDeliveryItems = dto.ContractDeliveryItems.Select((item, index) => new ContractDeliveryItem
                {
                    DeliveryItemId = Guid.NewGuid(),
                    DeliveryItemCode = $"DLI-{index + 1:D3}-{deliveryBatchCode}",
                    DeliveryBatchId = batchId,
                    ContractItemId = item.ContractItemId,
                    PlannedQuantity = item.PlannedQuantity ?? 0,
                    Note = item.Note,
                    CreatedAt = DateHelper.NowVietnamTime(),
                    UpdatedAt = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                }).ToList()
            };

            return deliveryBatch;
        }

        // Mapper ContractDeliveryBatchUpdateDto -> ContractDeliveryBatch
        public static void MapToUpdatedContractDeliveryBatch(
            this ContractDeliveryBatchUpdateDto dto,
            ContractDeliveryBatch contractDeliveryBatch)
        {
            contractDeliveryBatch.ContractId = dto.ContractId;
            contractDeliveryBatch.DeliveryRound = dto.DeliveryRound;
            contractDeliveryBatch.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            contractDeliveryBatch.TotalPlannedQuantity = dto.TotalPlannedQuantity;
            contractDeliveryBatch.Status = dto.Status.ToString();
            contractDeliveryBatch.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
