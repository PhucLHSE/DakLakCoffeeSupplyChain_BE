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
    public class ProcessingBatchRepository : GenericRepository<ProcessingBatch>, IProcessingBatchRepository
    {
        public ProcessingBatchRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<List<ProcessingBatch>> GetAll()
        {
            return await _context.ProcessingBatches
                .Include(b => b.Farmer)
                    .ThenInclude(f => f.User)
                .Include(b => b.Method)
                .Where(b => !b.IsDeleted)
                .ToListAsync();
        }

    }
}
