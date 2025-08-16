using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
                    cdb.CreatedAt.HasValue &&
                    cdb.CreatedAt.Value.Year == year
                );
        }

        public async Task<int> CountAsync(Expression<Func<ContractDeliveryBatch, bool>>? predicate = null)
        {
            IQueryable<ContractDeliveryBatch> query = _context.ContractDeliveryBatches.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);

            return await query.CountAsync();
        }

        // Sum cho double?
        public async Task<double?> SumAsync(
            Expression<Func<ContractDeliveryBatch, double?>> selector,
            Expression<Func<ContractDeliveryBatch, bool>>? predicate = null)
        {
            IQueryable<ContractDeliveryBatch> query = _context.ContractDeliveryBatches.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);

            return await query.SumAsync(selector);
        }

        // Sum cho decimal?
        public async Task<decimal?> SumAsync(
            Expression<Func<ContractDeliveryBatch, decimal?>> selector,
            Expression<Func<ContractDeliveryBatch, bool>>? predicate = null)
        {
            IQueryable<ContractDeliveryBatch> query = _context.ContractDeliveryBatches.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);

            return await query.SumAsync(selector);
        }
    }
}
