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
    public class ContractDeliveryItemRepository : GenericRepository<ContractDeliveryItem>, IContractDeliveryItemRepository
    {
        public ContractDeliveryItemRepository() { }

        public ContractDeliveryItemRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Tính tổng PlannedQuantity của tất cả DeliveryItem thuộc một ContractItem cụ thể (chưa bị xóa mềm)
        public async Task<double> SumPlannedQuantityAsync(Guid contractItemId)
        {
            return await _context.ContractDeliveryItems
                .Where(cdi => cdi.ContractItemId == contractItemId && !cdi.IsDeleted)
                .SumAsync(cdi => (double?)cdi.PlannedQuantity) ?? 0;
        }

        // Tính tổng PlannedQuantity của tất cả DeliveryItem thuộc ContractItemId (trừ một DeliveryItem cụ thể)
        public async Task<double> SumPlannedQuantityAsync(
            Guid contractItemId, 
            Guid excludeDeliveryItemId)
        {
            return await _context.ContractDeliveryItems
                .Where(cdi =>
                    cdi.ContractItemId == contractItemId &&
                    cdi.DeliveryItemId != excludeDeliveryItemId &&
                    !cdi.IsDeleted)
                .SumAsync(cdi => (double?)cdi.PlannedQuantity) ?? 0;
        }

        // Đếm số item trong danh mục giao của hợp đồng
        public async Task<int> CountByDeliveryBatchIdAsync(Guid deliveryBatchId)
        {
            return await _context.ContractDeliveryItems
                .Where(item => item.DeliveryBatchId == deliveryBatchId &&
                               !item.IsDeleted)
                .CountAsync();
        }

        // NEW: Tổng PlannedQuantity theo từng ContractItemId của một hợp đồng
        public async Task<Dictionary<Guid, double>> SumPlannedByContractGroupedAsync(Guid contractId)
        {
            // Join sang bảng batch để lọc theo ContractId và loại bỏ batch bị xóa mềm
            var rows = await _context.ContractDeliveryItems
                .AsNoTracking()
                .Join(
                    _context.ContractDeliveryBatches.AsNoTracking(),
                    item => item.DeliveryBatchId,
                    batch => batch.DeliveryBatchId,
                    (item, batch) => new { item, batch }
                )
                .Where(x =>
                    !x.item.IsDeleted &&
                    !x.batch.IsDeleted &&
                    x.batch.ContractId == contractId
                )
                .GroupBy(x => x.item.ContractItemId)
                .Select(g => new
                {
                    ContractItemId = g.Key,
                    Qty = g.Sum(x => (double?)(x.item.PlannedQuantity)) ?? 0
                })
                .ToListAsync();

            return rows.ToDictionary(x => x.ContractItemId, x => x.Qty);
        }
    }
}
