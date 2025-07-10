using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcessingBatchWasteRepository : GenericRepository<ProcessingBatchWaste>, IProcessingBatchWasteRepository
    {
        public ProcessingBatchWasteRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<List<ProcessingBatchWaste>> GetAllWastesAsync()
        {
            return await _context.ProcessingBatchWastes
                .Include(w => w.Progress)
                   .ThenInclude(p => p.Stage)
                .AsNoTracking()
                .Where(w => !w.IsDeleted)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProcessingBatchWaste?> GetWasteByIdAsync(Guid wasteId)
        {
            return await _context.ProcessingBatchWastes
                .Include(w => w.Progress)
                   .ThenInclude(p => p.Stage)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WasteId == wasteId && !w.IsDeleted);
        }

        public async Task<int> CountByProgressIdAsync(Guid progressId)
        {
            return await _context.ProcessingBatchWastes
                .CountAsync(w => w.ProgressId == progressId && !w.IsDeleted);
        }
        public async Task<int> CountCreatedInYearAsync(int year)
        {
            return await _context.ProcessingBatchWastes
                .CountAsync(w => !w.IsDeleted && w.CreatedAt.Year == year);
        }
    }
}
