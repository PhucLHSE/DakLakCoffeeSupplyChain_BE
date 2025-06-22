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
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DetailId == cropSeasonDetailId && !d.IsDeleted);
        }

    }
}
