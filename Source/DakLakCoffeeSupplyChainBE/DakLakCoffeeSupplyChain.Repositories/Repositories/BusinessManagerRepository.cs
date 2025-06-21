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
    public class BusinessManagerRepository : GenericRepository<BusinessManager>, IBusinessManagerRepository
    {
        public BusinessManagerRepository() { }

        public BusinessManagerRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Lấy BusinessManager theo UserId (chỉ bản ghi chưa bị xóa mềm)
        public async Task<BusinessManager?> GetByUserIdAsync(Guid userId)
        {
            return await _context.BusinessManagers
                                 .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted);
        }

        // Lấy BusinessManager theo mã số thuế (TaxId)
        public async Task<BusinessManager?> GetByTaxIdAsync(string taxId)
        {
            return await _context.BusinessManagers
                .AsNoTracking()
                .FirstOrDefaultAsync(bm => bm.TaxId == taxId && !bm.IsDeleted);
        }

        // Đếm số BusinessManager đã đăng ký trong một năm
        public async Task<int> CountBusinessManagersRegisteredInYearAsync(int year)
        {
            return await _context.BusinessManagers
                .AsNoTracking()
                .CountAsync(bm => bm.CreatedAt.Year == year && !bm.IsDeleted);
        }

        public async Task<BusinessManager?> FindByUserIdAsync(Guid userId)
        {
            return await _context.BusinessManagers
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);
        }
    }
}
