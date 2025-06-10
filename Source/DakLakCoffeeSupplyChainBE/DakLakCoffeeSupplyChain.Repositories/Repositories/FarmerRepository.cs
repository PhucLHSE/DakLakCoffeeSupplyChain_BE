using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class FarmerRepository : GenericRepository<Farmer>, IFarmerRepository
    {
        public FarmerRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<Farmer?> GetByIdAsync(Guid id)
        {
            return await _context.Farmers
                .AsNoTracking()
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FarmerId == id);
        }
    }
}
