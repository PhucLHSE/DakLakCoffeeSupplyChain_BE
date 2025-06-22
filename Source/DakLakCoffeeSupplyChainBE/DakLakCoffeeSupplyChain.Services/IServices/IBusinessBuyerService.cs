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

        Task<IServiceResult> GetById(Guid buyerId);

        Task<IServiceResult> Create(BusinessBuyerCreateDto businessBuyerDto, Guid userId);

        Task<IServiceResult> Update(BusinessBuyerUpdateDto businessBuyerDto);

        Task<IServiceResult> DeleteById(Guid buyerId);

        Task<IServiceResult> SoftDeleteById(Guid buyerId);
    }
}
