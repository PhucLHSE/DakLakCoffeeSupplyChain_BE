using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface ICultivationRegistrationsDetailRepository : IGenericRepository<CultivationRegistrationsDetail>
    {
        Task<List<CultivationRegistrationsDetail>> GetByRegistrationIdAsync(Guid registrationId);
    }
}
