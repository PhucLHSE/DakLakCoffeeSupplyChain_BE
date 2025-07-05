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
    public interface IBusinessManagerService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid managerId, Guid userId, string userRole);

        Task<IServiceResult> Create(BusinessManagerCreateDto businessManagerDto, Guid userId);

        Task<IServiceResult> Update(BusinessManagerUpdateDto businessManagerDto);

        Task<IServiceResult> DeleteBusinessManagerById(Guid managerId);

        Task<IServiceResult> SoftDeleteBusinessManagerById(Guid managerId, Guid userId, string userRole);
    }
}
