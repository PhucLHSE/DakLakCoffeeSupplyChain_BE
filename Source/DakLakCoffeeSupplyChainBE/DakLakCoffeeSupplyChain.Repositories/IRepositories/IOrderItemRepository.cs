using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IOrderItemRepository : IGenericRepository<OrderItem>
    {
        // Tính tổng Quantity của tất cả OrderItem thuộc cùng ContractDeliveryItem (để kiểm tra vượt PlannedQuantity)
        Task<double> SumQuantityByContractDeliveryItemAsync(Guid contractDeliveryItemId);
    }
}
