using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class CoffeeTypeRepository : GenericRepository<CoffeeType>, ICoffeeTypeRepository
    {
        public CoffeeTypeRepository(DakLakCoffee_SCMContext context) 
            => _context = context;

        public async Task<int> CountCoffeeTypeInYearAsync(int year)
        {
            return await _context.CoffeeTypes
                .CountAsync(p => 
                   p.CreatedAt.HasValue && 
                   p.CreatedAt.Value.Year == year
                );
        }
    }
}
