using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IContractDeliveryBatchRepository : IGenericRepository<ContractDeliveryBatch>
    {
        // Đếm số lô giao hàng hợp đồng được tạo trong năm chỉ định
        Task<int> CountByYearAsync(int year);
    }
}
