using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

public class CropSeasonRepository : GenericRepository<CropSeason>, ICropSeasonRepository
{
    public CropSeasonRepository(DakLakCoffee_SCMContext context) : base(context)
    {
    }

    public async Task<List<CropSeason>> GetAllCropSeasonsAsync()
    {
        return await _context.CropSeasons
            .AsNoTracking()
            .Include(c => c.Farmer)
            .OrderBy(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<CropSeason?> GetCropSeasonByIdAsync(Guid cropSeasonId)
    {
        return await _context.CropSeasons
            .AsNoTracking()
            .Include(c => c.Farmer)
            .FirstOrDefaultAsync(c => c.CropSeasonId == cropSeasonId);
    }
}
