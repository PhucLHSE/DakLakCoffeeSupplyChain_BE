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
    public class ShipmentRepository : GenericRepository<Shipment>, IShipmentRepository
    {
        public ShipmentRepository() { }

        public ShipmentRepository(DakLakCoffee_SCMContext context)
            => _context = context;

        // Đếm số shipment được tạo trong năm chỉ định, chưa bị xoá.
        public async Task<int> CountShipmentsInYearAsync(int year)
        {
            return await _context.Shipments
                .CountAsync(s =>
                    s.CreatedAt.Year == year &&
                    !s.IsDeleted);
        }
    }
}
