using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IAgriculturalExpertRepository : IGenericRepository<AgriculturalExpert>
    {
        Task<int> CountVerifiedExpertsAsync();
        Task<AgriculturalExpert?> GetByUserIdAsync(Guid userId);
        Task<List<AgriculturalExpert>> GetAllAsync(
            Func<AgriculturalExpert, bool>? predicate = null,
            Func<IQueryable<AgriculturalExpert>, IQueryable<AgriculturalExpert>>? include = null,
            Func<IQueryable<AgriculturalExpert>, IOrderedQueryable<AgriculturalExpert>>? orderBy = null,
            bool asNoTracking = false);
        
        // Thêm method để kiểm tra trùng lặp
        Task<AgriculturalExpert?> GetByExpertiseAreaAsync(string expertiseArea);
        
        // Đếm số chuyên gia đã đăng ký trong một năm
        Task<int> CountExpertsRegisteredInYearAsync(int year);
    }
}
