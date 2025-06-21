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
    public class BusinessBuyerRepository : GenericRepository<BusinessBuyer>, IBusinessBuyerRepository
    {
        public BusinessBuyerRepository() { }

        public BusinessBuyerRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Đếm số BusinessBuyer đã được tạo trong một năm
        public async Task<int> CountBuyersCreatedByManagerInYearAsync(Guid managerId, int year)
        {
            return await _context.BusinessBuyers
                .Where(b => b.CreatedBy == managerId &&
                            b.CreatedAt.Year == year &&
                            !b.IsDeleted)
                .CountAsync();
        }
    }
}
