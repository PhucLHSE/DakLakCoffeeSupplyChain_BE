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
    public class WarehouseReceiptRepository : GenericRepository<WarehouseReceipt>, IWarehouseReceiptRepository
    {
        public WarehouseReceiptRepository(DakLakCoffee_SCMContext context) : base(context)
        {
        }

        public async Task<List<WarehouseReceipt>> GetByInboundRequestIdAsync(Guid inboundRequestId)
        {
            var receipts = await _context.WarehouseReceipts
                .Where(r => r.InboundRequestId == inboundRequestId && !r.IsDeleted)
                .ToListAsync();
            
            // ✅ Sử dụng helper method để xử lý an toàn
            return receipts.OrderBy(r => GetSafeDateTime(r.ReceivedAt, DateTime.MaxValue)).ToList();
        }

        public async Task<List<WarehouseReceipt>> GetAllWithIncludesAsync()
        {
            var receipts = await _context.WarehouseReceipts
                .Where(r => !r.IsDeleted)
                .Include(r => r.Warehouse)
                .Include(r => r.Batch)
                    .ThenInclude(b => b.CoffeeType)
                .Include(r => r.Detail)  // Thêm Detail cho cà phê tươi
                    .ThenInclude(d => d.CropSeason)
                .Include(r => r.Detail)
                    .ThenInclude(d => d.CommitmentDetail)
                        .ThenInclude(cd => cd.PlanDetail)
                            .ThenInclude(pd => pd.CoffeeType)
                .Include(r => r.ReceivedByNavigation)
                   .ThenInclude(s => s.User)
                .ToListAsync();
            
            // ✅ Sử dụng helper method để xử lý an toàn
            return receipts.OrderByDescending(r => GetSafeDateTime(r.ReceivedAt, DateTime.MinValue)).ToList();
        }

        public async Task<WarehouseReceipt?> GetDetailByIdAsync(Guid id)
        {
            var receipt = await _context.WarehouseReceipts
                .Include(r => r.Warehouse)
                .Include(r => r.Batch)
                    .ThenInclude(b => b.CoffeeType)
                .Include(r => r.Detail)  // Thêm Detail cho cà phê tươi
                    .ThenInclude(d => d.CropSeason)
                .Include(r => r.Detail)
                    .ThenInclude(d => d.CommitmentDetail)
                        .ThenInclude(cd => cd.PlanDetail)
                            .ThenInclude(pd => pd.CoffeeType)
                .Include(r => r.InboundRequest) // ✅ Thêm để lấy số lượng yêu cầu nhập
                .Include(r => r.ReceivedByNavigation)
                   .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => 
                   r.ReceiptId == id && 
                   !r.IsDeleted
                );

            if (receipt != null && receipt.InboundRequest != null)
            {
                // Tính toán số lượng còn lại thực tế
                var existingReceipts = await _context.WarehouseReceipts
                    .Where(r => r.InboundRequestId == receipt.InboundRequestId && !r.IsDeleted)
                    .ToListAsync();
                
                double totalReceivedSoFar = existingReceipts.Sum(r => r.ReceivedQuantity ?? 0);
                double remainingQuantity = (receipt.InboundRequest.RequestedQuantity ?? 0) - totalReceivedSoFar;
                
                // Thêm thông tin vào receipt object (sử dụng dynamic hoặc anonymous object)
                // Vì WarehouseReceipt là entity, ta sẽ sử dụng cách khác
            }

            return receipt;
        }

        public async Task<int> CountCreatedInYearAsync(int year)
        {
            var receipts = await _context.WarehouseReceipts
                .Where(r => !r.IsDeleted)
                .ToListAsync();
            
            // ✅ Sử dụng helper method để xử lý an toàn
            return receipts.Count(r => {
                var safeDate = GetSafeDateTime(r.ReceivedAt, DateTime.MinValue);
                return safeDate.Year == year;
            });
        }

        // ✅ HELPER METHOD: Xử lý DateTime an toàn
        private DateTime GetSafeDateTime(DateTime? dateTime, DateTime defaultValue)
        {
            try
            {
                if (dateTime.HasValue && 
                    dateTime.Value > DateTime.MinValue && 
                    dateTime.Value < DateTime.MaxValue &&
                    dateTime.Value.Year > 1900 && 
                    dateTime.Value.Year < 2100)
                {
                    return dateTime.Value;
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public IQueryable<WarehouseReceipt> GetQuery()
        {
            return _context.WarehouseReceipts.AsQueryable();
        }
    }
}
