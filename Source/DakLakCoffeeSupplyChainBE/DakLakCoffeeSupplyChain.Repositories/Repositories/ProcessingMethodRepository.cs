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
    public class ProcessingMethodRepository : GenericRepository<ProcessingMethod>, IProcessingMethodRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public ProcessingMethodRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ProcessingMethod?> GetDetailByMethodIdAsync(int methodId)
        {
            return await _context.ProcessingMethods
                .Include(m => m.ProcessingStages)
                .FirstOrDefaultAsync(m => m.MethodId == methodId);
        }

        public async Task<bool> IsMethodInUseAsync(int methodId)
        {
            return await _context.ProcessingBatches.AnyAsync(b => b.MethodId == methodId);
        }
    }
}
