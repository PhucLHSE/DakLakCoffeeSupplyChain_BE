using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class AgriculturalExpertRepository : GenericRepository<AgriculturalExpert>, IAgriculturalExpertRepository
    {
        public AgriculturalExpertRepository() { }

        public AgriculturalExpertRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        public async Task<int> CountVerifiedExpertsAsync()
        {
            return await _context.AgriculturalExperts
                .AsNoTracking()
                .CountAsync(e => e.IsVerified == true && !e.IsDeleted);
        }
    }
}
