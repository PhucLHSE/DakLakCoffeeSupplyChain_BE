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
    public class ContractDeliveryBatchRepository : GenericRepository<ContractDeliveryBatch>, IContractDeliveryBatchRepository
    {
        public ContractDeliveryBatchRepository() { }

        public ContractDeliveryBatchRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Đếm số lô giao hàng hợp đồng chưa bị xóa được tạo trong năm chỉ định
        public async Task<int> CountByYearAsync(int year)
        {
            return await _context.ContractDeliveryBatches
                .CountAsync(cdb =>
                    !cdb.IsDeleted &&
                    cdb.CreatedAt.HasValue &&
                    cdb.CreatedAt.Value.Year == year
                );
        }
    }
}
