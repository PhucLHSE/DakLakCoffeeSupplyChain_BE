using DakLakCoffeeSupplyChain.Repositories.Base;
using DakLakCoffeeSupplyChain.Repositories.DBContext;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Repositories.Repositories
{
    public class GeneralFarmerReportRepository : GenericRepository<GeneralFarmerReport>, IGeneralFarmerReportRepository
    {
        public GeneralFarmerReportRepository(DakLakCoffee_SCMContext context) : base(context) { }

        public async Task<List<GeneralFarmerReport>> GetAllWithIncludesAsync()
        {
            return await _context.GeneralFarmerReports
                .Where(r => !r.IsDeleted)
                .Include(r => r.ReportedByNavigation)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();
        }

        public async Task<GeneralFarmerReport?> GetByIdWithIncludesAsync(Guid reportId)
        {
            return await _context.GeneralFarmerReports
                .Include(r => r.ReportedByNavigation)
                .Include(r => r.CropProgress)
                    .ThenInclude(cp => cp.Stage)
                .Include(r => r.ProcessingProgress)
                    .ThenInclude(pp => pp.Batch)
                .FirstOrDefaultAsync(r => r.ReportId == reportId && !r.IsDeleted);
        }

        public async Task<int> CountReportsInYearAsync(int year)
        {
            return await _context.GeneralFarmerReports
                .Where(r => !r.IsDeleted && r.ReportedAt.Year == year)
                .CountAsync();
        }
        public async Task<GeneralFarmerReport?> GetByIdEvenIfDeletedAsync(Guid reportId)
        {
            return await _context.GeneralFarmerReports
                .IgnoreQueryFilters() // nếu dùng Global Filter
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }
        public async Task<string?> GetMaxReportCodeForYearAsync(int year)
        {
            return await _context.GeneralFarmerReports
                .Where(r => r.ReportCode.StartsWith($"RPT-{year}-"))
                .OrderByDescending(r => r.ReportCode)
                .Select(r => r.ReportCode)
                .FirstOrDefaultAsync();
        }
        public void Update(GeneralFarmerReport entity)
        {
            _context.GeneralFarmerReports.Update(entity);
        }

    }
}
