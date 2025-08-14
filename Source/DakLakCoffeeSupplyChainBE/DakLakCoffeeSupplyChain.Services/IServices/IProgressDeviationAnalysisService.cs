using DakLakCoffeeSupplyChain.Common.DTOs.ProgressDeviationAnalysisDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProgressDeviationAnalysisService
    {
        /// <summary>
        /// Phân tích sai lệch tiến độ cho một mùa vụ cụ thể
        /// </summary>
        Task<IServiceResult> AnalyzeCropSeasonDeviationAsync(Guid cropSeasonId, Guid userId, bool isAdmin = false, bool isManager = false);

        /// <summary>
        /// Phân tích sai lệch tiến độ cho một vùng trồng cụ thể
        /// </summary>
        Task<IServiceResult> AnalyzeCropSeasonDetailDeviationAsync(Guid cropSeasonDetailId, Guid userId, bool isAdmin = false, bool isManager = false);

        /// <summary>
        /// Phân tích sai lệch tiến độ tổng hợp cho tất cả mùa vụ của nông dân
        /// </summary>
        Task<IServiceResult> AnalyzeFarmerOverallDeviationAsync(Guid userId);

        /// <summary>
        /// Phân tích sai lệch tiến độ tổng hợp cho toàn bộ hệ thống (Admin/Manager)
        /// </summary>
        Task<IServiceResult> AnalyzeSystemOverallDeviationAsync(bool isAdmin = false, bool isManager = false);

        /// <summary>
        /// Tạo báo cáo sai lệch tiến độ định kỳ
        /// </summary>
        Task<IServiceResult> GenerateDeviationReportAsync(DateTime fromDate, DateTime toDate, Guid? farmerId = null, bool isAdmin = false, bool isManager = false);
    }
}
