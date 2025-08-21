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
                .FirstOrDefaultAsync(e => 
                   e.UserId == userId && 
                   !e.IsDeleted
                );
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

        public async Task<int> CountVerifiedExpertsAsync()
        {
            return await _context.AgriculturalExperts
                .CountAsync(e => 
                   e.IsVerified == true && 
                   !e.IsDeleted
                );
        }

        // Thêm method để kiểm tra trùng lặp theo lĩnh vực chuyên môn
        public async Task<AgriculturalExpert?> GetByExpertiseAreaAsync(string expertiseArea)
        {
            return await _context.AgriculturalExperts
                .AsNoTracking()
                .FirstOrDefaultAsync(e => 
                   e.ExpertiseArea == expertiseArea && 
                   !e.IsDeleted
                );
        }

        // Đếm số chuyên gia đã đăng ký trong một năm
        public async Task<int> CountExpertsRegisteredInYearAsync(int year)
        {
            return await _context.AgriculturalExperts
                .AsNoTracking()
                .CountAsync(e => 
                   e.CreatedAt.Year == year && 
                   !e.IsDeleted
                );
        }
    }
}
