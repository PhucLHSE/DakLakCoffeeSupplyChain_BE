﻿using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
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
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid userId);

        Task<IServiceResult> Create(UserAccountCreateDto userDto);

        Task<IServiceResult> Update(UserAccountUpdateDto userDto);

        Task<IServiceResult> DeleteById(Guid userId);

        Task<IServiceResult> SoftDeleteById(Guid userId);
    }
}
