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
    public class ContractRepository : GenericRepository<Contract>, IContractRepository
    {
        public ContractRepository() { }

        public ContractRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Đếm số Contract đã tạo trong một năm
        public async Task<int> CountContractsInYearAsync(int year)
        {
            return await _context.Contracts
                .AsNoTracking()
                .CountAsync(c => c.CreatedAt.Year == year);
        }
    }
}
