using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        // Đếm số đơn hàng được tạo trong năm chỉ định, chưa bị xoá.
        Task<int> CountOrdersInYearAsync(int year);
    }
}
