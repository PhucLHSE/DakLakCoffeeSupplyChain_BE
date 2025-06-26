using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractDeliveryBatchEnums;
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
        public static ContractDeliveryBatchViewAllDto MapToContractDeliveryBatchViewAllDto(this ContractDeliveryBatch contractDeliveryBatch)
        {
            // Parse Status string từ entity sang enum
            ContractDeliveryBatchStatus status = Enum.TryParse<ContractDeliveryBatchStatus>(
                contractDeliveryBatch.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : ContractDeliveryBatchStatus.Planned;

            return new ContractDeliveryBatchViewAllDto
            {
                DeliveryBatchId = contractDeliveryBatch.DeliveryBatchId,
                DeliveryBatchCode = contractDeliveryBatch.DeliveryBatchCode ?? string.Empty,
                ContractId = contractDeliveryBatch.ContractId,
                DeliveryRound = contractDeliveryBatch.DeliveryRound,
                ExpectedDeliveryDate = contractDeliveryBatch.ExpectedDeliveryDate,
                TotalPlannedQuantity = contractDeliveryBatch.TotalPlannedQuantity,
                Status = status,
                CreatedAt = contractDeliveryBatch.CreatedAt,
                UpdatedAt = contractDeliveryBatch.UpdatedAt
            };
        }
    }
}
