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

        // Tính tổng PlannedQuantity của tất cả DeliveryItem thuộc ContractItemId (trừ một DeliveryItem cụ thể)
        Task<double> SumPlannedQuantityAsync(Guid contractItemId, Guid excludeDeliveryItemId);

        // Đếm số item trong danh mục giao của hợp đồng
        Task<int> CountByDeliveryBatchIdAsync(Guid deliveryBatchId);

        // Tổng PlannedQuantity theo từng ContractItemId của 1 hợp đồng (gộp tất cả đợt, bỏ soft-delete)
        Task<Dictionary<Guid, double>> SumPlannedByContractGroupedAsync(Guid contractId);

        // Tính tổng FulfilledQuantity của tất cả DeliveryItem thuộc một DeliveryBatch cụ thể
        Task<double> SumFulfilledQuantityByBatchAsync(Guid deliveryBatchId);

        // Tính tổng PlannedQuantity của tất cả DeliveryItem thuộc một DeliveryBatch cụ thể
        Task<double> SumPlannedQuantityByBatchAsync(Guid deliveryBatchId);

        // Tính tổng PlannedQuantity của tất cả DeliveryItem thuộc một DeliveryBatch cụ thể (trừ một item)
        Task<double> SumPlannedQuantityByBatchAsync(Guid deliveryBatchId, Guid excludeDeliveryItemId);
    }
}
