using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CultivationRegistrationsDetailRepository : GenericRepository<CultivationRegistrationsDetail>, ICultivationRegistrationsDetailRepository
    {
        public CultivationRegistrationsDetailRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<List<CultivationRegistrationsDetail>> GetByRegistrationIdAsync(Guid registrationId)
        {
            return await _context.CultivationRegistrationsDetails
                .Where(x => x.RegistrationId == registrationId && !x.IsDeleted)
                .ToListAsync();
        }
    }
}
