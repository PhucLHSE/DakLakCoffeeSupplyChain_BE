using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CropSeasonDetailRepository : GenericRepository<CropSeasonDetail>, ICropSeasonDetailRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public CropSeasonDetailRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<CropSeasonDetail>> GetByCropSeasonIdAsync(Guid cropSeasonId)
        {
            return await _context.CropSeasonDetails
                .Include(d => d.CommitmentDetail)
                    .ThenInclude(d => d.PlanDetail)
                .Where(d => d.CropSeasonId == cropSeasonId && !d.IsDeleted)
                .ToListAsync();
        }

        public async Task<CropSeasonDetail?> GetByIdAsync(Guid detailId)
        {
            return await _context.CropSeasonDetails
         .Include(d => d.CommitmentDetail)
            .ThenInclude(d => d.PlanDetail)
         .Include(d => d.CropSeason)
            .ThenInclude(cs => cs.Farmer) // nếu bạn muốn lấy FarmerName từ entity Farmer
         .FirstOrDefaultAsync(d => d.DetailId == detailId && !d.IsDeleted);
        }

        public async Task<bool> ExistsAsync(
            Expression<Func<CropSeasonDetail, bool>> predicate)
        {
            return await _context.CropSeasonDetails
                .AnyAsync(predicate);
        }

        public async Task<CropSeasonDetail?> GetDetailWithIncludesAsync(Guid detailId)
        {
            return await _context.CropSeasonDetails
                .Include(d => d.CommitmentDetail)
                    .ThenInclude(d => d.PlanDetail)
                        .ThenInclude(d => d.CoffeeType)
                .Include(d => d.CropSeason)
                    .ThenInclude(cs => cs.Farmer)
                        .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(d => d.DetailId == detailId && !d.IsDeleted);
        }
    }
}
