using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class AgriculturalExpertRepository : GenericRepository<AgriculturalExpert>, IAgriculturalExpertRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public AgriculturalExpertRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AgriculturalExpert?> GetByUserIdAsync(Guid userId)
        {
            return await _context.AgriculturalExperts
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted);
        }

        public async Task<List<AgriculturalExpert>> GetAllAsync(
            Func<AgriculturalExpert, bool>? predicate = null,
            Func<IQueryable<AgriculturalExpert>, IQueryable<AgriculturalExpert>>? include = null,
            Func<IQueryable<AgriculturalExpert>, IOrderedQueryable<AgriculturalExpert>>? orderBy = null,
            bool asNoTracking = false)
        {
            var query = _context.AgriculturalExperts.AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            if (orderBy != null)
                query = orderBy(query);

            var result = await query.ToListAsync();

            if (predicate != null)
                result = result.Where(predicate).ToList();

            return result;
        }
    }
}
