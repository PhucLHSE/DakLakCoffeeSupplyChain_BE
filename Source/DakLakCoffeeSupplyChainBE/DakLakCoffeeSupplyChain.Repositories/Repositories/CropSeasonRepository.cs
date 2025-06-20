using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class CropSeasonRepository : GenericRepository<CropSeason>, ICropSeasonRepository
{
    public CropSeasonRepository(DakLakCoffee_SCMContext context)
            => _context = context;

    public async Task<List<CropSeason>> GetAllCropSeasonsAsync()
    {
        return await _context.CropSeasons
            .AsNoTracking()
            .Include(c => c.Farmer)
                .ThenInclude(f => f.User)
            .OrderBy(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<CropSeason?> GetCropSeasonByIdAsync(Guid cropSeasonId)
    {
        return await _context.CropSeasons
            .AsNoTracking()
            .Include(c => c.Farmer)
              .ThenInclude(f => f.User)
            .FirstOrDefaultAsync(c => c.CropSeasonId == cropSeasonId);
    }
    public async Task<int> CountByYearAsync(int year)
    {
        return await _context.CropSeasons
            .CountAsync(c => c.StartDate.HasValue && c.StartDate.Value.Year == year);
    }
    public async Task<CropSeason?> GetWithDetailsByIdAsync(Guid cropSeasonId)
    {
        return await _context.CropSeasons
            .Include(cs => cs.CropSeasonDetails)
            .FirstOrDefaultAsync(cs => cs.CropSeasonId == cropSeasonId);
    }

    public async Task DeleteCropSeasonDetailsBySeasonIdAsync(Guid cropSeasonId)
    {
        var details = await _context.CropSeasonDetails
            .Where(d => d.CropSeasonId == cropSeasonId)
            .ToListAsync();

        _context.CropSeasonDetails.RemoveRange(details);
    }
    public async Task<bool> ExistsAsync(Expression<Func<CropSeason, bool>> predicate)
    {
        return await _context.CropSeasons.AnyAsync(predicate);
    }

}
