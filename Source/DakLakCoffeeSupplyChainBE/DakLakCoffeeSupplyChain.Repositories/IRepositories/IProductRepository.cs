using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<List<Product>> GetAllProductsAsync();

        Task<Product?> GetProductByIdAsync(Guid productId);

        // Đếm số sản phẩm do BusinessManager tạo trong năm (lọc theo IsDeleted và CreatedBy).
        Task<int> CountByManagerIdInYearAsync(Guid managerId, int year);
    }
}
