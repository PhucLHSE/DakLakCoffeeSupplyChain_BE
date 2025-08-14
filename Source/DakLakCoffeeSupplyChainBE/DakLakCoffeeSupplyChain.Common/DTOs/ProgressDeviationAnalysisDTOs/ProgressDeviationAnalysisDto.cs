using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProgressDeviationAnalysisDTOs
{
    /// <summary>
    /// DTO chính cho phân tích sai lệch tiến độ
    /// </summary>
    public class ProgressDeviationAnalysisDto
    {
        public Guid AnalysisId { get; set; }
        public string AnalysisCode { get; set; }
        public Guid CropSeasonId { get; set; }
        public Guid? CropSeasonDetailId { get; set; }
        public string CropSeasonName { get; set; }
        public string CropSeasonDetailName { get; set; }
        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; }
        public DateTime AnalysisDate { get; set; }
        public DateTime? ExpectedStartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public double? ExpectedYield { get; set; }
        public double? ActualYield { get; set; }
        public int ExpectedTotalStages { get; set; }
        public int CompletedStages { get; set; }
        public int CurrentStageIndex { get; set; }
        public string CurrentStageName { get; set; }
        public string CurrentStageCode { get; set; }
        public double ProgressPercentage { get; set; }
        public double ExpectedProgressPercentage { get; set; }
        public double DeviationPercentage { get; set; }
        public string DeviationStatus { get; set; } // OnTime, Ahead, Behind, Critical
        public string DeviationLevel { get; set; } // Low, Medium, High, Critical
        public int DaysAhead { get; set; }
        public int DaysBehind { get; set; }
        public double YieldDeviationPercentage { get; set; }
        public List<StageDeviationDto> StageDeviations { get; set; } = new List<StageDeviationDto>();
        public List<RecommendationDto> Recommendations { get; set; } = new List<RecommendationDto>();
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO cho sai lệch từng giai đoạn
    /// </summary>
    public class StageDeviationDto
    {
        public int StageId { get; set; }
        public string StageName { get; set; }
        public string StageCode { get; set; }
        public int OrderIndex { get; set; }
        public DateTime? ExpectedStartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public int DaysAhead { get; set; }
        public int DaysBehind { get; set; }
        public string DeviationStatus { get; set; }
        public string DeviationLevel { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// DTO cho khuyến nghị cải thiện
    /// </summary>
    public class RecommendationDto
    {
        public string Category { get; set; } // Timing, Yield, Quality, Process
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } // Low, Medium, High, Critical
        public string Impact { get; set; } // Low, Medium, High
        public string Effort { get; set; } // Low, Medium, High
        public List<string> Actions { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO cho báo cáo tổng hợp sai lệch
    /// </summary>
    public class OverallDeviationReportDto
    {
        public DateTime ReportDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalCropSeasons { get; set; }
        public int OnTimeSeasons { get; set; }
        public int AheadSeasons { get; set; }
        public int BehindSeasons { get; set; }
        public int CriticalSeasons { get; set; }
        public double AverageDeviationPercentage { get; set; }
        public double AverageYieldDeviationPercentage { get; set; }
        public List<ProgressDeviationAnalysisDto> TopDeviations { get; set; } = new List<ProgressDeviationAnalysisDto>();
        public List<ProgressDeviationAnalysisDto> CriticalDeviations { get; set; } = new List<ProgressDeviationAnalysisDto>();
        public Dictionary<string, int> DeviationByRegion { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DeviationByCropType { get; set; } = new Dictionary<string, int>();
    }
}
