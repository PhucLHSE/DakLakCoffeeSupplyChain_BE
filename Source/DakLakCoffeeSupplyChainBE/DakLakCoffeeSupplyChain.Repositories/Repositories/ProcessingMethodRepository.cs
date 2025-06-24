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
    public class ProcessingMethodRepository : GenericRepository<ProcessingMethod>, IProcessingMethodRepository
    {
        private readonly DakLakCoffee_SCMContext _context;

        public ProcessingMethodRepository(DakLakCoffee_SCMContext context) : base(context)
        {
            _context = context;
        }
        public async Task<bool> SoftDeleteAsync(int methodId)
        {
            var method = await _context.ProcessingMethods
                .FirstOrDefaultAsync(m => m.MethodId == methodId && !m.IsDeleted);

            if (method == null)
                return false;

            method.IsDeleted = true;
            method.UpdatedAt = DateTime.UtcNow;

            _context.ProcessingMethods.Update(method);
            return true;
        }

    }
}
