using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
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
    }
}
