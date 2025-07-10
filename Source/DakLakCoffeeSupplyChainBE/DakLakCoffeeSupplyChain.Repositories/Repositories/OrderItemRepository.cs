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
    public class OrderItemRepository : GenericRepository<OrderItem>, IOrderItemRepository
    {
        public OrderItemRepository() { }

        public OrderItemRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Tính tổng Quantity của tất cả OrderItem thuộc cùng ContractDeliveryItem (để kiểm tra vượt PlannedQuantity)
        public async Task<double> SumQuantityByContractDeliveryItemAsync(Guid contractDeliveryItemId)
        {
            return await _context.OrderItems
                .Where(oi => 
                   oi.ContractDeliveryItemId == contractDeliveryItemId && 
                   !oi.IsDeleted
                )
                .SumAsync(oi => (double?)oi.Quantity) ?? 0.0;
        }
    }
}
