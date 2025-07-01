using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.IRepositories.DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;

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
        public async Task<ProcessingBatchProgress?> GetByIdAsync(Guid id)
        {
            return await _context.ProcessingBatchProgresses
                .Include(p => p.Stage)
                .Include(p => p.Batch)
                .Include(p => p.UpdatedByNavigation)
                    .ThenInclude(f => f.User)
                .Include(p => p.ProcessingParameters)
                .FirstOrDefaultAsync(p => p.ProgressId == id && !p.IsDeleted);
        }
        public async Task<bool> UpdateAsync(ProcessingBatchProgress entity)
        {
            try
            {
                _context.ProcessingBatchProgresses.Update(entity);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> SoftDeleteAsync(Guid progressId)
        {
            var entity = await _context.ProcessingBatchProgresses
                .FirstOrDefaultAsync(p => p.ProgressId == progressId && !p.IsDeleted);

            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.ProcessingBatchProgresses.Update(entity); // cần thiết để EF ghi nhận thay đổi
            return true;
        }
        public async Task<bool> HardDeleteAsync(Guid progressId)
        {
            var entity = await _context.ProcessingBatchProgresses
                .FirstOrDefaultAsync(p => p.ProgressId == progressId);

            if (entity == null)
                return false;

            _context.ProcessingBatchProgresses.Remove(entity);
            return true;
        }
    }
}