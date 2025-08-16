using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IContractDeliveryBatchRepository : IGenericRepository<ContractDeliveryBatch>
    {
        // Đếm số lô giao hàng hợp đồng được tạo trong năm chỉ định
        Task<int> CountByYearAsync(int year);

        Task<int> CountAsync(Expression<Func<ContractDeliveryBatch, bool>>? predicate = null);

        // Sum cho double?
        Task<double?> SumAsync(
            Expression<Func<ContractDeliveryBatch, double?>> selector,
            Expression<Func<ContractDeliveryBatch, bool>>? predicate = null
        );

        // Sum cho decimal?
        Task<decimal?> SumAsync(
            Expression<Func<ContractDeliveryBatch, decimal?>> selector,
            Expression<Func<ContractDeliveryBatch, bool>>? predicate = null
        );
    }
}
