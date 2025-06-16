using DakLakCoffeeSupplyChain.Common.DTOs.RoleDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IRoleService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(int roleId);

        Task<IServiceResult> Create(RoleCreateDto roleDto);

        Task<IServiceResult> Update(RoleUpdateDto roleDto);

        Task<IServiceResult> DeleteById(int roleId);
    }
}
