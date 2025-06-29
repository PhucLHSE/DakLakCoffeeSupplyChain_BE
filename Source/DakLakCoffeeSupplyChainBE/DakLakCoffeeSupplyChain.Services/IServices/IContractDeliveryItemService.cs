using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IContractDeliveryItemService
    {
        Task<IServiceResult> Create(ContractDeliveryItemCreateDto contractDeliveryItemDto);

        Task<IServiceResult> Update(ContractDeliveryItemUpdateDto contractDeliveryItemDto);

        Task<IServiceResult> DeleteContractDeliveryItemById(Guid deliveryItemId);

        Task<IServiceResult> SoftDeleteContractDeliveryItemById(Guid deliveryItemId);
    }
}
