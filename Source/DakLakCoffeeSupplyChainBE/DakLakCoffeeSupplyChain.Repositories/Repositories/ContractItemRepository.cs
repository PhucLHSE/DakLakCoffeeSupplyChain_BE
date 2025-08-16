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
    public class ContractItemRepository : GenericRepository<ContractItem>, IContractItemRepository
    {
        public ContractItemRepository() { }

        public ContractItemRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Đếm số item trong hợp đồng
        public async Task<int> CountByContractIdAsync(Guid contractId)
        {
            return await _context.ContractItems
                .Where(item => item.ContractId == contractId)
                .CountAsync();
        }
    }
}
