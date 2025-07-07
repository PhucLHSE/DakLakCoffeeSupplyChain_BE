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

        public async Task<int> CountStaffCreatedInYearAsync(int year)
        {
            return await _context.BusinessStaffs
                .AsNoTracking()
                .CountAsync(bs => bs.CreatedAt.Year == year && !bs.IsDeleted);
        }

        public async Task<BusinessStaff?> GetByIdWithUserAsync(Guid staffId)
        {
            return await _context.BusinessStaffs
                .Include(bs => bs.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.StaffId == staffId && !bs.IsDeleted);
        }

        public async Task<List<BusinessStaff>> GetBySupervisorIdAsync(Guid supervisorId)
        {
            return await _context.BusinessStaffs
                .Include(bs => bs.User)
                .Where(bs => bs.SupervisorId == supervisorId && !bs.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        // Lấy BusinessStaff theo UserId (chỉ bản ghi chưa bị xóa mềm)
        public async Task<BusinessStaff?> GetByUserIdAsync(Guid userId)
        {
            return await _context.BusinessStaffs
                                 .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted);
        }
    }
}
