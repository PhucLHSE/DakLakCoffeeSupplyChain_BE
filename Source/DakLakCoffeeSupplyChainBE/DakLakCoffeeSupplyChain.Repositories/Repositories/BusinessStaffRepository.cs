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
    public class BusinessStaffRepository : GenericRepository<BusinessStaff>, IBusinessStaffRepository
    {
        public BusinessStaffRepository(DakLakCoffee_SCMContext context) : base(context) { }
        public async Task<List<BusinessStaff>> GetAllWithUserAsync()
        {
            return await _context.BusinessStaffs
                .Include(bs => bs.User)
                .ToListAsync();
        }
        public async Task<BusinessStaff?> FindByUserIdAsync(Guid userId)
        {
            return await _context.BusinessStaffs
                .Include(bs => bs.User)
                .FirstOrDefaultAsync(bs => bs.UserId == userId);
        }


    }
}
