using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.IRepositories.DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcessingBatchProgressRepository : GenericRepository<ProcessingBatchProgress>, IProcessingBatchProgressRepository
    {
        public ProcessingBatchProgressRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<List<ProcessingBatchProgress>> GetAllWithIncludesAsync()
        {
            return await _context.ProcessingBatchProgresses
                .Include(p => p.Stage)
                .Include(p => p.Batch)
                .Include(p => p.UpdatedByNavigation)
                    .ThenInclude(f => f.User)
                .Include(p => p.ProcessingParameters)
                .Include(p => p.ProcessingBatchWastes)
                .Where(p => !p.IsDeleted)
                .ToListAsync();
        }
    }
}