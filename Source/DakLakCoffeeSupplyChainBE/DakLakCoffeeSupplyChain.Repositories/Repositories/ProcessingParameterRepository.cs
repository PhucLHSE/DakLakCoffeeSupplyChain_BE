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
    public class ProcessingParameterRepository : GenericRepository<ProcessingParameter>, IProcessingParameterRepository
    {
        public ProcessingParameterRepository() { }

        public ProcessingParameterRepository(DakLakCoffee_SCMContext context)
        {
            _context = context;
        }

        public async Task<List<ProcessingParameter>> GetAllActiveAsync()
        {
            return await _context.ProcessingParameters
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.RecordedAt)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<ProcessingParameter?> GetByIdAsync(Guid parameterId)
        {
            return await _context.ProcessingParameters
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParameterId == parameterId && !p.IsDeleted);
        }
        public async Task<bool> SoftDeleteAsync(Guid parameterId)
        {
            var entity = await _context.ProcessingParameters
                .FirstOrDefaultAsync(p => p.ParameterId == parameterId && !p.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.ProcessingParameters.Update(entity);
            return true;
        }
        public async Task<bool> HardDeleteAsync(Guid parameterId)
        {
            var entity = await _context.ProcessingParameters
                .FirstOrDefaultAsync(p => p.ParameterId == parameterId);

            if (entity == null)
                return false;

            _context.ProcessingParameters.Remove(entity);
            return true;
        }

    }
}
