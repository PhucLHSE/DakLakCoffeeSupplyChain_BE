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
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository() { }

        public ProductRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.CoffeeType)
                .Include(p => p.Batch)
                .Include(p => p.Inventory)
                   .ThenInclude(p => p.Warehouse)
                .OrderBy(p => p.ProductCode)
                .ToListAsync();

            return products;
        }

        public async Task<Product?> GetProductByIdAsync(Guid productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.CoffeeType)
                .Include(p => p.Batch)
                .Include(p => p.Inventory)
                   .ThenInclude(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            return product;
        }

        // Đếm số sản phẩm do BusinessManager tạo trong năm (lọc theo IsDeleted và CreatedBy).
        public async Task<int> CountByManagerIdInYearAsync(Guid managerId, int year)
        {
            // Lấy tất cả UserId thuộc doanh nghiệp (manager + staff)
            var allUserIds = await _context.UserAccounts
                .Where(u => !u.IsDeleted &&
                           (
                               _context.BusinessManagers.Any(m =>
                                   m.UserId == u.UserId && m.ManagerId == managerId && !m.IsDeleted)
                               ||
                               _context.BusinessStaffs.Any(s =>
                                   s.UserId == u.UserId && s.SupervisorId == managerId && !s.IsDeleted)
                           )
                )
                .Select(u => u.UserId)
                .ToListAsync();

            return await _context.Products.CountAsync(p =>
               p.CreatedAt.Year == year &&
               allUserIds.Contains(p.CreatedBy)
            );
        }
    }
}
