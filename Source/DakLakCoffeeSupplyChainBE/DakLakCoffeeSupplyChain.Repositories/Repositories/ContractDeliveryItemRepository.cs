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

        // Đếm số item trong danh mục giao của hợp đồng
        public async Task<int> CountByDeliveryBatchIdAsync(Guid deliveryBatchId)
        {
            return await _context.ContractDeliveryItems
                .Where(item => item.DeliveryBatchId == deliveryBatchId &&
                               !item.IsDeleted)
                .CountAsync();
        }
    }
}
