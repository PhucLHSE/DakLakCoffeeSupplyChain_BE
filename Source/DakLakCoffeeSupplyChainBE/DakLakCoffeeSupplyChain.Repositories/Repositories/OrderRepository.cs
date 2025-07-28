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
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository() { }

        public OrderRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Đếm số đơn hàng được tạo trong năm chỉ định, chưa bị xoá.
        public async Task<int> CountOrdersInYearAsync(int year)
        {
            return await _context.Orders
                .CountAsync(o =>
                    o.CreatedAt.Year == year &&
                    !o.IsDeleted);
        }
    }
}
