using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IContractDeliveryBatchService
    {
        Task<IServiceResult> GetAll(Guid userId);

        Task<IServiceResult> GetById(Guid deliveryBatchId, Guid userId);

        Task<IServiceResult> Create(ContractDeliveryBatchCreateDto contractDeliveryBatchDto, Guid userId);

        Task<IServiceResult> Update(ContractDeliveryBatchUpdateDto contractDeliveryBatchDto, Guid userId);

        Task<IServiceResult> DeleteContractDeliveryBatchById(Guid deliveryBatchId);

        Task<IServiceResult> SoftDeleteContractDeliveryBatchById(Guid deliveryBatchId);
    }
}
