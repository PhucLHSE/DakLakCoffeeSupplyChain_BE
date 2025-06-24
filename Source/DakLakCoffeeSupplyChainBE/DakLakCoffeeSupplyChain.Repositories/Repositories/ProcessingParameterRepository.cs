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
    }
}
