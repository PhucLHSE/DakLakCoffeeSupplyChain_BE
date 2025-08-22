using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProgressDeviationAnalysisDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProgressDeviationEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    // Progress Deviation Analysis Service - Phân tích sai lệch tiến độ mùa vụ
    public class ProgressDeviationAnalysisService : IProgressDeviationAnalysisService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        // Constants for deviation thresholds
        private const double LOW_DEVIATION_THRESHOLD = 10.0;      // 0-10%
        private const double MEDIUM_DEVIATION_THRESHOLD = 25.0;   // 10-25%
        private const double HIGH_DEVIATION_THRESHOLD = 50.0;     // 25-50%
        private const int CRITICAL_DAYS_THRESHOLD = 30;           // 30+ days behind

        public ProgressDeviationAnalysisService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> AnalyzeCropSeasonDeviationAsync(Guid cropSeasonId, Guid userId, bool isAdmin = false, bool isManager = false)
        {
            try
            {
                // Get crop season with details
                var cropSeason = await _unitOfWork.CropSeasonRepository.GetWithDetailsByIdAsync(cropSeasonId);
                if (cropSeason == null || cropSeason.IsDeleted)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy mùa vụ.");

                // Check permissions
                if (!isAdmin && !isManager && cropSeason.Farmer.UserId != userId)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không có quyền truy cập mùa vụ này.");

                var analysis = await AnalyzeCropSeasonDeviationInternalAsync(cropSeason);
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, analysis);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi phân tích sai lệch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> AnalyzeCropSeasonDetailDeviationAsync(Guid cropSeasonDetailId, Guid userId, bool isAdmin = false, bool isManager = false)
        {
            try
            {
                // Get crop season detail with progress
                var detail = await _unitOfWork.CropSeasonDetailRepository.GetByIdAsync(
                    predicate: d => d.DetailId == cropSeasonDetailId && !d.IsDeleted,
                    include: q => q
                        .Include(d => d.CropSeason)
                            .ThenInclude(cs => cs.Farmer)
                                .ThenInclude(f => f.User)
                        .Include(d => d.CropProgresses.Where(p => !p.IsDeleted))
                            .ThenInclude(p => p.Stage),
                    asNoTracking: true
                );

                if (detail == null)
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy vùng trồng.");

                // Check permissions
                if (!isAdmin && !isManager && detail.CropSeason.Farmer.UserId != userId)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không có quyền truy cập vùng trồng này.");

                var analysis = await AnalyzeCropSeasonDetailDeviationInternalAsync(detail);
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, analysis);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi phân tích sai lệch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> AnalyzeFarmerOverallDeviationAsync(Guid userId)
        {
            try
            {
                // Use existing method to get crop seasons by user
                var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                    predicate: cs => !cs.IsDeleted && cs.Farmer.UserId == userId,
                    include: q => q
                        .Include(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                        .Include(cs => cs.CropSeasonDetails)
                            .ThenInclude(d => d.CropProgresses.Where(p => !p.IsDeleted))
                                .ThenInclude(p => p.Stage),
                    asNoTracking: true
                );

                if (!cropSeasons.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có mùa vụ nào để phân tích.");

                var analyses = new List<ProgressDeviationAnalysisDto>();
                foreach (var season in cropSeasons)
                {
                    var analysis = await AnalyzeCropSeasonDeviationInternalAsync(season);
                    analyses.Add(analysis);
                }

                var overallReport = GenerateOverallDeviationReport(analyses, DateTime.Now.AddMonths(-6), DateTime.Now);
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, overallReport);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi phân tích tổng hợp: {ex.Message}");
            }
        }

        public async Task<IServiceResult> AnalyzeSystemOverallDeviationAsync(bool isAdmin = false, bool isManager = false)
        {
            try
            {
                if (!isAdmin && !isManager)
                    return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Không có quyền truy cập báo cáo hệ thống.");

                var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                    predicate: cs => !cs.IsDeleted,
                    include: q => q
                        .Include(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                        .Include(cs => cs.CropSeasonDetails)
                            .ThenInclude(d => d.CropProgresses.Where(p => !p.IsDeleted))
                                .ThenInclude(p => p.Stage),
                    asNoTracking: true
                );

                if (!cropSeasons.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có mùa vụ nào để phân tích.");

                var analyses = new List<ProgressDeviationAnalysisDto>();
                foreach (var season in cropSeasons)
                {
                    var analysis = await AnalyzeCropSeasonDeviationInternalAsync(season);
                    analyses.Add(analysis);
                }

                var overallReport = GenerateOverallDeviationReport(analyses, DateTime.Now.AddMonths(-6), DateTime.Now);
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, overallReport);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi phân tích hệ thống: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GenerateDeviationReportAsync(DateTime fromDate, DateTime toDate, Guid? farmerId = null, bool isAdmin = false, bool isManager = false)
        {
            try
            {
                var from = DateOnly.FromDateTime(fromDate);
                var to = DateOnly.FromDateTime(toDate);

                Expression<Func<CropSeason, bool>> predicate;
                if (isAdmin || isManager)
                {
                    predicate = cs => !cs.IsDeleted && cs.StartDate >= from && cs.StartDate <= to;
                }
                else
                {
                    if (farmerId == null)
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Thiếu userId người gọi (farmerId).");

                    var callerUserId = farmerId.Value;
                    predicate = cs => !cs.IsDeleted && cs.StartDate >= from && cs.StartDate <= to
                                      && cs.Farmer.UserId == callerUserId;
                }

                var cropSeasons = await _unitOfWork.CropSeasonRepository.GetAllAsync(
                    predicate: predicate,
                    include: q => q
                        .Include(cs => cs.Farmer)
                            .ThenInclude(f => f.User)
                        .Include(cs => cs.CropSeasonDetails)
                            .ThenInclude(d => d.CropProgresses.Where(p => !p.IsDeleted))
                                .ThenInclude(p => p.Stage),
                    asNoTracking: true
                );

                if (!cropSeasons.Any())
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không có dữ liệu trong khoảng thời gian này.");

                var analyses = new List<ProgressDeviationAnalysisDto>();
                foreach (var season in cropSeasons)
                {
                    var analysis = await AnalyzeCropSeasonDeviationInternalAsync(season);
                    analyses.Add(analysis);
                }

                var report = GenerateOverallDeviationReport(analyses, fromDate, toDate);
                return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, report);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, $"Lỗi tạo báo cáo: {ex.Message}");
            }
        }

        #region Private Methods

        private async Task<ProgressDeviationAnalysisDto> AnalyzeCropSeasonDeviationInternalAsync(CropSeason cropSeason)
        {
            var now = DateHelper.NowVietnamTime();

            var analysis = new ProgressDeviationAnalysisDto
            {
                AnalysisId = Guid.NewGuid(),
                AnalysisCode = await GenerateDeviationAnalysisCodeAsync(),
                CropSeasonId = cropSeason.CropSeasonId,
                CropSeasonName = cropSeason.SeasonName,
                FarmerId = cropSeason.FarmerId,
                FarmerName = cropSeason.Farmer?.User?.Name ?? cropSeason.Farmer?.User?.Email ?? "Unknown",
                AnalysisDate = now,
                ExpectedStartDate = cropSeason.StartDate?.ToDateTime(TimeOnly.MinValue),
                ExpectedEndDate = cropSeason.EndDate?.ToDateTime(TimeOnly.MinValue),
                CreatedAt = now,
                UpdatedAt = now
            };

            // Analyze each detail
            var detailAnalyses = new List<ProgressDeviationAnalysisDto>();
            foreach (var detail in cropSeason.CropSeasonDetails.Where(d => !d.IsDeleted))
            {
                var detailAnalysis = await AnalyzeCropSeasonDetailDeviationInternalAsync(detail);
                detailAnalyses.Add(detailAnalysis);
            }

            // Aggregate detail analyses
            if (detailAnalyses.Any())
            {
                analysis.ExpectedTotalStages = Math.Max(detailAnalyses.Max(d => d.ExpectedTotalStages), 1);
                analysis.CompletedStages = detailAnalyses.Sum(d => d.CompletedStages);
                analysis.CurrentStageIndex = detailAnalyses.Max(d => d.CurrentStageIndex);
                analysis.ProgressPercentage = detailAnalyses.Average(d => d.ProgressPercentage);

                analysis.ExpectedProgressPercentage = CalculateExpectedProgressPercentage(cropSeason.StartDate, cropSeason.EndDate);
                analysis.DeviationPercentage = analysis.ProgressPercentage - analysis.ExpectedProgressPercentage;

                // DaysBehind: lấy lớn nhất giữa (max theo stage của detail) và (quy đổi từ % chênh)
                var daysByDetails = detailAnalyses.Max(d => d.DaysBehind);
                var daysByPctGap = EstimateDaysFromPercentageGap(
                    analysis.ExpectedProgressPercentage, analysis.ProgressPercentage,
                    analysis.ExpectedTotalStages,
                    cropSeason.StartDate, cropSeason.EndDate);

                analysis.DaysBehind = Math.Max(daysByDetails, daysByPctGap);

                analysis.DeviationStatus = DetermineDeviationStatus(analysis.DeviationPercentage, analysis.DaysBehind);
                analysis.DeviationLevel = DetermineDeviationLevel(Math.Abs(analysis.DeviationPercentage));
                analysis.StageDeviations = detailAnalyses.SelectMany(d => d.StageDeviations).ToList();
                analysis.Recommendations = GenerateRecommendations(analysis);
            }

            return analysis;
        }

        private async Task<ProgressDeviationAnalysisDto> AnalyzeCropSeasonDetailDeviationInternalAsync(CropSeasonDetail detail)
        {
            var now = DateHelper.NowVietnamTime();

            var analysis = new ProgressDeviationAnalysisDto
            {
                AnalysisId = Guid.NewGuid(),
                AnalysisCode = await GenerateDeviationAnalysisCodeAsync(),
                CropSeasonDetailId = detail.DetailId,
                CropSeasonDetailName = $"{detail.CropSeason.SeasonName} - {detail.CommitmentDetail?.PlanDetail?.CoffeeType?.TypeName ?? "Unknown"}",
                ExpectedYield = detail.EstimatedYield,
                ActualYield = detail.ActualYield,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Suy ra số stage kỳ vọng từ dữ liệu (fallback 5)
            var derivedStages = detail.CropProgresses
                .Select(p => (int?)p.StageId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .Count();
            analysis.ExpectedTotalStages = derivedStages > 0 ? derivedStages : 5;

            // Analyze progress
            var progresses = detail.CropProgresses
                .OrderBy(p => p.StepIndex ?? int.MaxValue)
                .ThenBy(p => p.ProgressDate)
                .ToList();

            analysis.CompletedStages = progresses.Count;
            analysis.CurrentStageIndex = progresses.Any() ? progresses.Max(p => p.StepIndex ?? 0) : 0;
            analysis.ProgressPercentage = analysis.ExpectedTotalStages > 0
                ? (double)analysis.CompletedStages / analysis.ExpectedTotalStages * 100.0
                : 0.0;

            // Calculate expected progress based on time
            if (detail.ExpectedHarvestStart.HasValue && detail.ExpectedHarvestEnd.HasValue)
            {
                analysis.ExpectedProgressPercentage = CalculateExpectedProgressPercentage(
                    detail.ExpectedHarvestStart.Value, detail.ExpectedHarvestEnd.Value);
            }

            // Analyze stage deviations
            analysis.StageDeviations = AnalyzeStageDeviations(progresses, detail);

            // Calculate overall deviation & days behind
            var maxStageLag = analysis.StageDeviations.Any() ? analysis.StageDeviations.Max(sd => sd.DaysBehind) : 0;
            var daysByPctGap = EstimateDaysFromPercentageGap(
                analysis.ExpectedProgressPercentage, analysis.ProgressPercentage,
                analysis.ExpectedTotalStages,
                detail.ExpectedHarvestStart, detail.ExpectedHarvestEnd);

            analysis.DaysBehind = Math.Max(maxStageLag, daysByPctGap);

            analysis.DeviationPercentage = analysis.ProgressPercentage - analysis.ExpectedProgressPercentage;
            analysis.DeviationStatus = DetermineDeviationStatus(analysis.DeviationPercentage, analysis.DaysBehind);
            analysis.DeviationLevel = DetermineDeviationLevel(Math.Abs(analysis.DeviationPercentage));

            // Calculate yield deviation
            if (analysis.ExpectedYield.HasValue && analysis.ActualYield.HasValue && analysis.ExpectedYield.Value != 0)
            {
                analysis.YieldDeviationPercentage = ((analysis.ActualYield.Value - analysis.ExpectedYield.Value) / analysis.ExpectedYield.Value) * 100.0;
            }

            // Generate recommendations
            analysis.Recommendations = GenerateRecommendations(analysis);

            return analysis;
        }

        private List<StageDeviationDto> AnalyzeStageDeviations(List<CropProgress> progresses, CropSeasonDetail detail)
        {
            var stageDeviations = new List<StageDeviationDto>();

            if (!detail.ExpectedHarvestStart.HasValue || !detail.ExpectedHarvestEnd.HasValue)
                return stageDeviations;

            var expectedStageDuration = CalculateExpectedStageDuration(detail);
            var baseStart = detail.ExpectedHarvestStart.Value;

            for (int i = 0; i < progresses.Count; i++)
            {
                var progress = progresses[i];
                var orderIndex = progress.StepIndex ?? (i + 1);

                var expectedStartDate = baseStart.AddDays((orderIndex - 1) * expectedStageDuration);
                var expectedEndDate = expectedStartDate.AddDays(expectedStageDuration);
                DateTime? actualDate = progress.ProgressDate?.ToDateTime(TimeOnly.MinValue);

                var deviation = new StageDeviationDto
                {
                    StageId = progress.StageId,
                    StageName = progress.Stage?.StageName,
                    StageCode = progress.Stage?.StageCode,
                    OrderIndex = orderIndex,
                    ExpectedStartDate = expectedStartDate.ToDateTime(TimeOnly.MinValue),
                    ExpectedEndDate = expectedEndDate.ToDateTime(TimeOnly.MinValue),
                    ActualStartDate = actualDate,
                    ActualEndDate = actualDate
                };

                // Calculate timing deviations
                if (actualDate.HasValue)
                {
                    var expStart = expectedStartDate.ToDateTime(TimeOnly.MinValue);
                    var daysDiff = (actualDate.Value - expStart).Days;

                    if (daysDiff < 0)
                    {
                        deviation.DaysAhead = Math.Abs(daysDiff);
                        deviation.DaysBehind = 0;
                        deviation.DeviationStatus = DeviationStatus.Ahead.ToString();
                    }
                    else if (daysDiff > 0)
                    {
                        deviation.DaysAhead = 0;
                        deviation.DaysBehind = daysDiff;
                        deviation.DeviationStatus = daysDiff > CRITICAL_DAYS_THRESHOLD
                            ? DeviationStatus.Critical.ToString()
                            : DeviationStatus.Behind.ToString();
                    }
                    else
                    {
                        deviation.DaysAhead = 0;
                        deviation.DaysBehind = 0;
                        deviation.DeviationStatus = DeviationStatus.OnTime.ToString();
                    }

                    var magnitude = Math.Max(deviation.DaysAhead, deviation.DaysBehind);
                    deviation.DeviationLevel = DetermineDayBasedLevel(magnitude);
                }
                else
                {
                    // Chưa có log thực tế → chưa kết luận lệch, coi như Low
                    deviation.DeviationStatus = DeviationStatus.OnTime.ToString();
                    deviation.DeviationLevel = DeviationLevel.Low.ToString();
                }

                stageDeviations.Add(deviation);
            }

            return stageDeviations;
        }

        private double CalculateExpectedProgressPercentage(DateOnly? startDate, DateOnly? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
                return 0;

            var startDateTime = startDate.Value.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDate.Value.ToDateTime(TimeOnly.MinValue);
            var totalDuration = (endDateTime - startDateTime).Days;
            if (totalDuration <= 0) return 0;

            var elapsedDays = (DateHelper.NowVietnamTime() - startDateTime).Days;
            if (elapsedDays < 0) elapsedDays = 0;
            if (elapsedDays > totalDuration) elapsedDays = totalDuration;

            return (double)elapsedDays / totalDuration * 100.0;
        }

        private int CalculateExpectedStageDuration(CropSeasonDetail detail)
        {
            if (!detail.ExpectedHarvestStart.HasValue || !detail.ExpectedHarvestEnd.HasValue)
                return 30; // Default 30 days per stage

            var startDateTime = detail.ExpectedHarvestStart.Value.ToDateTime(TimeOnly.MinValue);
            var endDateTime = detail.ExpectedHarvestEnd.Value.ToDateTime(TimeOnly.MinValue);
            var totalDuration = (endDateTime - startDateTime).Days;

            // Ưu tiên số stage suy ra từ dữ liệu, fallback 5
            var inferredStages = detail.CropProgresses
                .Select(p => (int?)p.StageId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .Count();

            var stages = Math.Max(inferredStages, 5);
            return Math.Max(1, totalDuration / stages);
        }

        private string DetermineDeviationStatus(double deviationPercentage, int daysBehind)
        {
            if (daysBehind > CRITICAL_DAYS_THRESHOLD)
                return DeviationStatus.Critical.ToString();

            if (deviationPercentage < -10)
                return DeviationStatus.Behind.ToString();
            else if (deviationPercentage > 10)
                return DeviationStatus.Ahead.ToString();
            else
                return DeviationStatus.OnTime.ToString();
        }

        private string DetermineDeviationLevel(double deviationPercentage)
        {
            if (deviationPercentage <= LOW_DEVIATION_THRESHOLD)
                return DeviationLevel.Low.ToString();
            else if (deviationPercentage <= MEDIUM_DEVIATION_THRESHOLD)
                return DeviationLevel.Medium.ToString();
            else if (deviationPercentage <= HIGH_DEVIATION_THRESHOLD)
                return DeviationLevel.High.ToString();
            else
                return DeviationLevel.Critical.ToString();
        }

        // Level dựa theo số ngày lệch (áp cho từng stage)
        private string DetermineDayBasedLevel(int days)
        {
            if (days <= 3) return DeviationLevel.Low.ToString();
            if (days <= 7) return DeviationLevel.Medium.ToString();
            if (days <= 14) return DeviationLevel.High.ToString();
            return DeviationLevel.Critical.ToString();
        }

        // Quy đổi khoảng cách % tiến độ thành số ngày (để ước lượng DaysBehind cho detail/season)
        private int EstimateDaysFromPercentageGap(
            double expectedPct, double actualPct, int expectedStages,
            DateOnly? startDate, DateOnly? endDate)
        {
            var pctGap = Math.Max(0.0, expectedPct - actualPct);
            if (pctGap <= 0.0 || expectedStages <= 0 || !startDate.HasValue || !endDate.HasValue)
                return 0;

            var totalDays = (endDate.Value.ToDateTime(TimeOnly.MinValue) - startDate.Value.ToDateTime(TimeOnly.MinValue)).Days;
            if (totalDays <= 0) return 0;

            return Math.Max(0, (int)Math.Round(pctGap / 100.0 * totalDays));
        }

        private List<RecommendationDto> GenerateRecommendations(ProgressDeviationAnalysisDto analysis)
        {
            var recommendations = new List<RecommendationDto>();

            // Timing recommendations
            if (analysis.DeviationStatus == DeviationStatus.Behind.ToString())
            {
                recommendations.Add(new RecommendationDto
                {
                    Category = RecommendationCategory.Timing.ToString(),
                    Title = "Tăng tốc tiến độ",
                    Description = "Cần đẩy nhanh tiến độ để đảm bảo thu hoạch đúng hạn",
                    Priority = RecommendationPriority.High.ToString(),
                    Impact = ImpactLevel.High.ToString(),
                    Effort = EffortLevel.Medium.ToString(),
                    Actions = new List<string>
                    {
                        "Tăng cường chăm sóc cây trồng",
                        "Sử dụng phân bón và thuốc bảo vệ thực vật hợp lý",
                        "Theo dõi thời tiết và điều chỉnh kế hoạch"
                    }
                });
            }

            // Yield recommendations
            if (analysis.YieldDeviationPercentage < -10)
            {
                recommendations.Add(new RecommendationDto
                {
                    Category = RecommendationCategory.Yield.ToString(),
                    Title = "Cải thiện sản lượng",
                    Description = "Cần áp dụng các biện pháp kỹ thuật để tăng sản lượng",
                    Priority = RecommendationPriority.Medium.ToString(),
                    Impact = ImpactLevel.Medium.ToString(),
                    Effort = EffortLevel.High.ToString(),
                    Actions = new List<string>
                    {
                        "Kiểm tra chất lượng đất và nước",
                        "Áp dụng quy trình canh tác tiên tiến",
                        "Tham khảo ý kiến chuyên gia nông nghiệp"
                    }
                });
            }

            // Process recommendations
            if (analysis.DeviationLevel == DeviationLevel.Critical.ToString())
            {
                recommendations.Add(new RecommendationDto
                {
                    Category = RecommendationCategory.Process.ToString(),
                    Title = "Đánh giá lại quy trình",
                    Description = "Cần xem xét lại toàn bộ quy trình canh tác",
                    Priority = RecommendationPriority.Critical.ToString(),
                    Impact = ImpactLevel.High.ToString(),
                    Effort = EffortLevel.High.ToString(),
                    Actions = new List<string>
                    {
                        "Phân tích nguyên nhân chậm tiến độ",
                        "Điều chỉnh kế hoạch canh tác",
                        "Tham khảo kinh nghiệm từ các mùa vụ thành công"
                    }
                });
            }

            return recommendations;
        }

        private OverallDeviationReportDto GenerateOverallDeviationReport(List<ProgressDeviationAnalysisDto> analyses, DateTime fromDate, DateTime toDate)
        {
            var report = new OverallDeviationReportDto
            {
                ReportDate = DateHelper.NowVietnamTime(),
                FromDate = fromDate,
                ToDate = toDate,
                TotalCropSeasons = analyses.Count,
                OnTimeSeasons = analyses.Count(a => a.DeviationStatus == DeviationStatus.OnTime.ToString()),
                AheadSeasons = analyses.Count(a => a.DeviationStatus == DeviationStatus.Ahead.ToString()),
                BehindSeasons = analyses.Count(a => a.DeviationStatus == DeviationStatus.Behind.ToString()),
                CriticalSeasons = analyses.Count(a => a.DeviationStatus == DeviationStatus.Critical.ToString()),
                AverageDeviationPercentage = analyses.Any() ? analyses.Average(a => Math.Abs(a.DeviationPercentage)) : 0,
                AverageYieldDeviationPercentage = analyses.Any() ? analyses.Average(a => Math.Abs(a.YieldDeviationPercentage)) : 0,
                TopDeviations = analyses.OrderByDescending(a => Math.Abs(a.DeviationPercentage)).Take(10).ToList(),
                CriticalDeviations = analyses.Where(a => a.DeviationStatus == DeviationStatus.Critical.ToString()).ToList()
            };

            return report;
        }

        private async Task<string> GenerateDeviationAnalysisCodeAsync()
        {
            // Tạm thời sinh code đơn giản (có thể thay bằng _codeGenerator sau)
            return $"DA-{DateHelper.NowVietnamTime():yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        #endregion
    }
}
