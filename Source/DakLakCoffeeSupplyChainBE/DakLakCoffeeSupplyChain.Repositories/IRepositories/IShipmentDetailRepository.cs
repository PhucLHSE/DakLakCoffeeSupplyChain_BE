using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Repositories.IRepositories
{
    public interface IShipmentDetailRepository : IGenericRepository<ShipmentDetail>
    {
        // Tính tổng số lượng đã giao (Quantity) của một OrderItem qua tất cả các dòng shipment chưa xoá
        Task<double> SumQuantityByOrderItemAsync(Guid orderItemId);

        // Tổng số lượng đã giao (giản lược, không kiểm tra HasValue)
        Task<double> GetDeliveredQuantityByOrderItemId(Guid orderItemId);

        // Tính tổng số lượng đã giao của một OrderItem, trừ một shipment nhất định
        Task<double> GetDeliveredQuantityByOrderItemId(Guid orderItemId, Guid? excludeShipmentId = null);
    }
}
