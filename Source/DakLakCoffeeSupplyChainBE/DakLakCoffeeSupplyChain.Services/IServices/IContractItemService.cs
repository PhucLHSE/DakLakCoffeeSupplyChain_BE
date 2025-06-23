using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IContractItemService
    {
        Task<IServiceResult> Create(ContractItemCreateDto contractItemDto);

        Task<IServiceResult> SoftDeleteContractItemById(Guid contractItemId);
    }
}
