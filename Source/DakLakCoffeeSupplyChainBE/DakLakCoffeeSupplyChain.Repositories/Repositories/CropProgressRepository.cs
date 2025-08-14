using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CropProgressRepository : GenericRepository<CropProgress>, ICropProgressRepository
    {
        public CropProgressRepository() { }

        public CropProgressRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        public async Task<List<CropProgress>> GetAllWithIncludesAsync()
        {
            return await _context.CropProgresses
                .AsNoTracking()
                .Include(p => p.Stage)
                .Include(p => p.UpdatedByNavigation)
                    .ThenInclude(f => f.User)
                .Include(p => p.CropSeasonDetail)
                    .ThenInclude(d => d.CropSeason)
                        .ThenInclude(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                .OrderByDescending(p => p.ProgressDate)
                .ToListAsync();
        }

        public async Task<List<CropProgress>> GetByCropSeasonDetailIdWithIncludesAsync(
            Guid cropSeasonDetailId, 
            Guid userId)
        {
            return await _context.CropProgresses
                .AsNoTracking()
                .Where(p => !p.IsDeleted &&
                            p.CropSeasonDetailId == cropSeasonDetailId &&
                            p.CropSeasonDetail.CropSeason.Farmer.UserId == userId)
                .Include(p => p.Stage)
                .Include(p => p.UpdatedByNavigation)
                   .ThenInclude(f => f.User)
                .Include(p => p.CropSeasonDetail)
                    .ThenInclude(d => d.CropSeason)
                        .ThenInclude(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                .OrderBy(p => p.StageId)
                .ThenBy(p => p.StepIndex ?? 0)
                .ThenBy(p => p.ProgressDate)
                .ToListAsync();
        }

        public async Task<List<CropProgress>> GetByCropSeasonDetailIdForManagerAsync(
            Guid cropSeasonDetailId)
        {
            return await _context.CropProgresses
                .AsNoTracking()
                .Where(p => !p.IsDeleted &&
                            p.CropSeasonDetailId == cropSeasonDetailId)
                .Include(p => p.Stage)
                .Include(p => p.UpdatedByNavigation)
                   .ThenInclude(f => f.User)
                .Include(p => p.CropSeasonDetail)
                    .ThenInclude(d => d.CropSeason)
                        .ThenInclude(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                .OrderBy(p => p.StageId)
                .ThenBy(p => p.StepIndex ?? 0)
                .ThenBy(p => p.ProgressDate)
                .ToListAsync();
        }

        public async Task<List<CropProgress>> FindAsync(
            Expression<Func<CropProgress, bool>> predicate)
        {
            return await _context.CropProgresses
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<CropProgress?> GetByIdWithDetailAsync(Guid progressId)
        {
            return await _context.CropProgresses
                .Include(p => p.CropSeasonDetail)
                    .ThenInclude(d => d.CropSeason)
                        .ThenInclude(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(p => 
                   p.ProgressId == progressId && 
                   !p.IsDeleted
                );
        }

        public async Task<CropProgress?> GetByIdWithIncludesAsync(Guid progressId)
        {
            return await _context.CropProgresses
                .Include(p => p.Stage)
                .Include(p => p.UpdatedByNavigation)
                   .ThenInclude(f => f.User)
                .Include(p => p.CropSeasonDetail)
                .FirstOrDefaultAsync(p => 
                   p.ProgressId == progressId && 
                   !p.IsDeleted
                );
        }
    }
}