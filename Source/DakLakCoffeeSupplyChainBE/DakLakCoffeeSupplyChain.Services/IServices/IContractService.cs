using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IContractService
    {
        Task<IServiceResult> GetAll(Guid userId);

        Task<IServiceResult> GetById(Guid contractId);

        Task<IServiceResult> Create(ContractCreateDto contractDto, Guid userId);

        Task<IServiceResult> DeleteContractById(Guid contractId);

        Task<IServiceResult> SoftDeleteContractById(Guid contractId);
    }
}
