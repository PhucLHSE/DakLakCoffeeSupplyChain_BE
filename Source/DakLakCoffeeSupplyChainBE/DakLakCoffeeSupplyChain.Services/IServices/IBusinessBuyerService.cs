using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IBusinessBuyerService
    {
        Task<IServiceResult> GetAll(Guid userId);

        Task<IServiceResult> GetById(Guid buyerId, Guid userId);

        Task<IServiceResult> Create(BusinessBuyerCreateDto businessBuyerDto, Guid userId);

        Task<IServiceResult> Update(BusinessBuyerUpdateDto businessBuyerDto, Guid userId);

        Task<IServiceResult> DeleteBusinessBuyerById(Guid buyerId, Guid userId);

        Task<IServiceResult> SoftDeleteBusinessBuyerById(Guid buyerId);
    }
}
