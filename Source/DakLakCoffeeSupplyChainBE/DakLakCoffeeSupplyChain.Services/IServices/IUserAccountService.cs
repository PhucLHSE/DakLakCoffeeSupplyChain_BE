using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IUserAccountService
    {
        Task<IServiceResult> GetAll(Guid userId, string userRole);

        Task<IServiceResult> GetById(Guid userId);

        Task<IServiceResult> Create(UserAccountCreateDto userDto, Guid userId, string userRole);

        Task<IServiceResult> Update(UserAccountUpdateDto userDto);

        Task<IServiceResult> DeleteUserAccountById(Guid userId, Guid currentUserId, string currentUserRole);

        Task<IServiceResult> SoftDeleteUserAccountById(Guid userId);

        Task<bool> CanAccessUser(Guid currentUserId, string currentUserRole, Guid targetUserId);
    }
}
