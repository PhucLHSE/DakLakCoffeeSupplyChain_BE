using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IContractDeliveryItemRepository : IGenericRepository<ContractDeliveryItem>
    {
        // Tính tổng PlannedQuantity của tất cả DeliveryItem thuộc một ContractItem cụ thể (chưa bị xóa mềm)
        Task<double> SumPlannedQuantityAsync(Guid contractItemId);

        // Đếm số item trong danh mục giao của hợp đồng
        Task<int> CountByDeliveryBatchIdAsync(Guid deliveryBatchId);
    }
}
