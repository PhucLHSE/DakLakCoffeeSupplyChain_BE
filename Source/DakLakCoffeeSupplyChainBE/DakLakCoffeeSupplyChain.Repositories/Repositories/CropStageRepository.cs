using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CropStageRepository : GenericRepository<CropStage>, ICropStageRepository
    {
        public CropStageRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<CropStage?> GetByCodeAsync(string code)
        {
            return await _context.CropStages
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StageCode == code);
        }

        public async Task<List<CropStage>> GetAllOrderedAsync()
        {
            return await _context.CropStages
                .AsNoTracking()
                .Where(s => !s.IsDeleted)  
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();
        }

        public async Task<CropStage?> GetByIdAsync(int stageId, bool asNoTracking)
        {
            var query = _context.CropStages.AsQueryable();

            if (asNoTracking) query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cs => cs.StageId == stageId);
        }
    }
}
