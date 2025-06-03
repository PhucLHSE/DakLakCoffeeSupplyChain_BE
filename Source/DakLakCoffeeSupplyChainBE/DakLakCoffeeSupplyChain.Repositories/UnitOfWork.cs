using DakLakCoffeeSupplyChain.Repositories.DBContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories
{
    public class UnitOfWork
    {
        private DakLakCoffee_SCMContext context;

        public UnitOfWork()
        {
            context ??= new DakLakCoffee_SCMContext();
        }
    }
}
