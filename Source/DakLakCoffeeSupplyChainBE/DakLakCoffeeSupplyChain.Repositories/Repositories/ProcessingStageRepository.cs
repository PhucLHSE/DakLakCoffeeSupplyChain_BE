using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class ProcessingStageRepository : GenericRepository<ProcessingStage>, IProcessingStageRepository
    {
        public ProcessingStageRepository() { }

        public ProcessingStageRepository(DakLakCoffee_SCMContext context)
        {
            _context = context;
        }

        public async Task<List<ProcessingStage>> GetAllStagesAsync()
        {
            return await _context.ProcessingStages
                .Include(s => s.Method)
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.MethodId)
                .ThenBy(s => s.OrderIndex)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task CreateAsync(ProcessingStage entity)
        {
            await _context.AddAsync(entity);
        }
    }
}
