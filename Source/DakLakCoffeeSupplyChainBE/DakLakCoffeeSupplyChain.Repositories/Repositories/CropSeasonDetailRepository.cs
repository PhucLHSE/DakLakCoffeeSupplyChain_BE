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
                .Include(d => d.CoffeeType)
                .Where(d => d.CropSeasonId == cropSeasonId && !d.IsDeleted)
                .ToListAsync();
        }

        public async Task<CropSeasonDetail?> GetByIdAsync(Guid detailId)
        {
            return await _context.CropSeasonDetails
                .Include(d => d.CoffeeType)
                .FirstOrDefaultAsync(d => d.DetailId == detailId && !d.IsDeleted);
        }
    }
}
