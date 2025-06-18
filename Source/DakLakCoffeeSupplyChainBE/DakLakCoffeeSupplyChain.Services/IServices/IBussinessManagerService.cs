using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IBussinessManagerService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid managerId);

        Task<IServiceResult> Create(BusinessManagerCreateDto businessManagerDto, Guid userId);
    }
}
