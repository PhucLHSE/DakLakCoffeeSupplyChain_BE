using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CultivationRegistrationRepository : GenericRepository<CultivationRegistration>, ICultivationRegistrationRepository
    {
        public CultivationRegistrationRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<CultivationRegistration?> GetByIdAsync(Guid id)
        {
            return await _context.CultivationRegistrations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RegistrationId == id);
        }

        public async Task<CropSeasonDetail?> GetCropSeasonDetailByIdAsync(Guid cropSeasonDetailId)
        {
            return await _context.CropSeasonDetails
                .Include(d => d.CropSeason)
                    .ThenInclude(cs => cs.Farmer)
                        .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(d => 
                   d.DetailId == cropSeasonDetailId && 
                   !d.IsDeleted
                );
        }

        public async Task<int> CountCultivationRegistrationsInYearAsync(int year)
        {
            return await _context.CultivationRegistrations
                .CountAsync(p => p.RegisteredAt.Year == year);
        }
    }
}
