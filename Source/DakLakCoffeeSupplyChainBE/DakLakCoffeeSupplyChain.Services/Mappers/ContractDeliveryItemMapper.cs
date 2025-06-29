using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class ContractDeliveryItemMapper
    {
        // Mapper ContractDeliveryItemViewDto
        public static ContractDeliveryItemViewDto MapToContractDeliveryItemViewDto(this ContractDeliveryItem contractDeliveryItem)
        {
            return new ContractDeliveryItemViewDto
            {
                DeliveryItemId = contractDeliveryItem.DeliveryItemId,
                DeliveryItemCode = contractDeliveryItem.DeliveryItemCode ?? string.Empty,
                ContractItemId = contractDeliveryItem.ContractItemId,
                CoffeeTypeName = contractDeliveryItem.ContractItem?.CoffeeType?.TypeName ?? string.Empty,
                PlannedQuantity = contractDeliveryItem.PlannedQuantity,
                FulfilledQuantity = contractDeliveryItem.FulfilledQuantity,
                Note = contractDeliveryItem.Note ?? string.Empty
            };
        }

        // Mapper ContractDeliveryItemCreateDto → ContractDeliveryItem
        public static ContractDeliveryItem MapToNewContractDeliveryItem(this ContractDeliveryItemCreateDto dto, string deliveryItemCode)
        {
            return new ContractDeliveryItem
            {
                DeliveryItemId = Guid.NewGuid(), // Tạo mới ID
                DeliveryItemCode = deliveryItemCode,
                DeliveryBatchId = dto.DeliveryBatchId,
                ContractItemId = dto.ContractItemId,
                PlannedQuantity = dto.PlannedQuantity ?? 0,
                FulfilledQuantity = 0,
                Note = dto.Note?.Trim(),
                CreatedAt = DateHelper.NowVietnamTime(), // Giờ Việt Nam
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Mapper ContractDeliveryItemUpdateDto → ContractDeliveryItem (cập nhật thông tin)
        public static void MapToUpdateContractDeliveryItem(this ContractDeliveryItemUpdateDto dto, ContractDeliveryItem contractDeliveryItem)
        {
            contractDeliveryItem.ContractItemId = dto.ContractItemId;
            contractDeliveryItem.PlannedQuantity = dto.PlannedQuantity ?? contractDeliveryItem.PlannedQuantity;
            contractDeliveryItem.FulfilledQuantity = dto.FulfilledQuantity;
            contractDeliveryItem.Note = dto.Note?.Trim();
            contractDeliveryItem.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
