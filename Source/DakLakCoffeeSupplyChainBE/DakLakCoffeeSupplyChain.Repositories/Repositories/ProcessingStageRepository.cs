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
            Console.WriteLine($"🔍 DEBUG: GetAllStagesAsync called");
            Console.WriteLine($"🔍 DEBUG: Context is null: {_context == null}");
            
            var stages = await _context.ProcessingStages
                .Include(s => s.Method)
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.MethodId)
                .ThenBy(s => s.OrderIndex)
                .AsNoTracking()
                .ToListAsync();
                
            Console.WriteLine($"🔍 DEBUG: Found {stages.Count} stages in repository");
            return stages;
        }

        public async Task CreateAsync(ProcessingStage entity)
        {
            await _context.AddAsync(entity);
        }

        public async Task<bool> SoftDeleteAsync(int stageId)
        {
            var stage = await _context.ProcessingStages
                .FirstOrDefaultAsync(s => s.StageId == stageId && !s.IsDeleted);

            if (stage == null)
                return false;

            stage.IsDeleted = true;
            stage.UpdatedAt = DateTime.UtcNow;

            _context.ProcessingStages.Update(stage);

            return true;
        }

        public async Task<bool> UpdateAsync(ProcessingStage entity)
        {
            var existing = await _context.ProcessingStages
                .FirstOrDefaultAsync(s => s.StageId == entity.StageId && !s.IsDeleted);

            if (existing == null)
                return false;

            existing.StageCode = entity.StageCode;
            existing.StageName = entity.StageName;
            existing.Description = entity.Description;
            existing.OrderIndex = entity.OrderIndex;
            existing.IsRequired = entity.IsRequired;
            existing.MethodId = entity.MethodId;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.ProcessingStages.Update(existing);

            return true;
        }
    }
}
