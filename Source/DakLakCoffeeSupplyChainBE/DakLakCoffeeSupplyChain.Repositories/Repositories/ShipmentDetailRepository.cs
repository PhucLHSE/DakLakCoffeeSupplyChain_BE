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
    public class ShipmentDetailRepository : GenericRepository<ShipmentDetail>, IShipmentDetailRepository
    {
        public ShipmentDetailRepository() { }

        public ShipmentDetailRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Tính tổng số lượng đã giao (Quantity) của một OrderItem qua tất cả các dòng shipment chưa xoá
        public async Task<double> SumQuantityByOrderItemAsync(Guid orderItemId)
        {
            return await _context.ShipmentDetails
                .Where(sd =>
                    sd.OrderItemId == orderItemId &&
                    !sd.IsDeleted &&
                    sd.Quantity.HasValue
                )
                .SumAsync(sd => (double?)sd.Quantity) ?? 0.0;
        }

        // Tổng số lượng đã giao (giản lược, không kiểm tra HasValue)
        public async Task<double> GetDeliveredQuantityByOrderItemId(Guid orderItemId)
        {
            return await _context.ShipmentDetails
                .Where(sd => 
                   sd.OrderItemId == orderItemId && 
                   !sd.IsDeleted
                )
                .SumAsync(d => (double?)d.Quantity) ?? 0.0;
        }

        // Tính tổng số lượng đã giao của một OrderItem, trừ một shipment nhất định (nếu có)
        public async Task<double> GetDeliveredQuantityByOrderItemId(Guid orderItemId, Guid? excludeShipmentId = null)
        {
            var query = _context.ShipmentDetails
                .Where(sd =>
                    sd.OrderItemId == orderItemId &&
                    !sd.IsDeleted
                );

            if (excludeShipmentId.HasValue)
            {
                query = query.Where(sd => sd.ShipmentId != excludeShipmentId.Value);
            }

            return await query.SumAsync(sd => (double?)sd.Quantity) ?? 0.0;
        }
    }
}
