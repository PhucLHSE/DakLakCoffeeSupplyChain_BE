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
        // Tối ưu: Sử dụng projection để chỉ lấy dữ liệu cần thiết
        return await _context.CropSeasons
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
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
            .Include(c => c.CropSeasonDetails)
                .ThenInclude(d => d.CommitmentDetail)
                   .ThenInclude(d => d.PlanDetail)
            .FirstOrDefaultAsync(c => c.CropSeasonId == cropSeasonId && !c.IsDeleted);
    }

    public async Task<List<CropSeason>> GetCropSeasonsByUserIdAsync(Guid userId)
    {
        // Tối ưu: Sử dụng projection để chỉ lấy dữ liệu cần thiết
        return await _context.CropSeasons
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Farmer.UserId == userId)
            .Include(c => c.Farmer)
                .ThenInclude(f => f.User)
            .OrderBy(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<int> CountByYearAsync(int year)
    {
        // Tối ưu: Sử dụng CountAsync với predicate để giảm dữ liệu truyền
        return await _context.CropSeasons
            .AsNoTracking()
            .CountAsync(c => 
               !c.IsDeleted &&
               c.StartDate.HasValue && 
               c.StartDate.Value.Year == year
            );
    }

    public async Task<CropSeason?> GetWithDetailsByIdAsync(Guid cropSeasonId)
    {
        // Tối ưu: Sử dụng projection để chỉ lấy dữ liệu cần thiết
        return await _context.CropSeasons
            .AsNoTracking()
            .Include(cs => cs.CropSeasonDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.CommitmentDetail)
                    .ThenInclude(d => d.PlanDetail)
                       .ThenInclude(pd => pd.CoffeeType) 
            .Include(cs => cs.Farmer)
                .ThenInclude(f => f.User)
            .Include(cs => cs.Commitment)
            .FirstOrDefaultAsync(cs => cs.CropSeasonId == cropSeasonId && !cs.IsDeleted);
    }

    // method dùng riêng cho Update, KHÔNG include CoffeeType
    public async Task<CropSeason?> GetWithDetailsByIdForUpdateAsync(Guid cropSeasonId)
    {
        // Tối ưu: Chỉ load dữ liệu cần thiết cho update
        return await _context.CropSeasons
            .Include(cs => cs.CropSeasonDetails.Where(d => !d.IsDeleted)) // chỉ cần lấy vùng trồng
            .Include(cs => cs.Farmer)
                .ThenInclude(f => f.User)
            .Include(cs => cs.Commitment)
            .FirstOrDefaultAsync(cs => 
               cs.CropSeasonId == cropSeasonId && 
               !cs.IsDeleted
            );
    }

    public async Task<CropSeason?> GetByIdAsync(Guid cropSeasonId)
    {
        return await _context.CropSeasons
            .Include(c => c.Farmer)
            .FirstOrDefaultAsync(c => 
               c.CropSeasonId == cropSeasonId && 
               !c.IsDeleted
            );
    }

    public async Task DeleteCropSeasonDetailsBySeasonIdAsync(Guid cropSeasonId)
    {
        var details = await _context.CropSeasonDetails
            .Where(d => d.CropSeasonId == cropSeasonId)
            .ToListAsync();

        _context.CropSeasonDetails.RemoveRange(details);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<CropSeason, bool>> predicate)
    {
        return await _context.CropSeasons
            .AnyAsync(predicate);
    }

    public IQueryable<CropSeason> GetQuery()
    {
        return _context.CropSeasons.AsQueryable();
    }
}
