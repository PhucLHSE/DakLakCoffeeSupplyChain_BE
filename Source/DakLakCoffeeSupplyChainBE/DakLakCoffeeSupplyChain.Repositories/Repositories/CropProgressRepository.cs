using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .OrderByDescending(p => p.ProgressDate)
                .ToListAsync();
        }

        public async Task<CropProgress?> GetByIdWithIncludesAsync(Guid progressId)
        {
            return await _context.CropProgresses
                .AsNoTracking()
                .Include(p => p.Stage)
                .Include(p => p.UpdatedByNavigation)
                    .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(p => p.ProgressId == progressId && !p.IsDeleted);
        }

    }
}
