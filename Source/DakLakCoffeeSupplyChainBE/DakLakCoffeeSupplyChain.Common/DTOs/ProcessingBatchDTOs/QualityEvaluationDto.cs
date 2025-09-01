using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs
{
    /// <summary>
    /// DTO cho tiêu chí đánh giá chất lượng từ SystemConfiguration
    /// </summary>
    public class QualityCriteriaDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string RuleGroup { get; set; } = string.Empty;
        public bool IsPassed { get; set; }
        public decimal? ActualValue { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho kết quả đánh giá chất lượng tổng thể
    /// </summary>
    public class QualityEvaluationResultDto
    {
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public List<QualityCriteriaDto> CriteriaResults { get; set; } = new List<QualityCriteriaDto>();
        public int TotalCriteria { get; set; }
        public int PassedCriteria { get; set; }
        public int FailedCriteria { get; set; }
        public decimal TotalScore { get; set; }
        public string OverallResult { get; set; } = string.Empty; // Pass, Fail, Conditional
        public string DecisionReason { get; set; } = string.Empty;
        public DateTime EvaluatedAt { get; set; }
        public Guid EvaluatedBy { get; set; }
        public string EvaluatorName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho việc tạo đánh giá chất lượng
    /// </summary>
    public class CreateQualityEvaluationDto
    {
        public Guid BatchId { get; set; }
        public List<QualityCriteriaEvaluationDto> CriteriaEvaluations { get; set; } = new List<QualityCriteriaEvaluationDto>();
        public string Comments { get; set; } = string.Empty;
        public string DecisionReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho việc đánh giá từng tiêu chí cụ thể
    /// </summary>
    public class QualityCriteriaEvaluationDto
    {
        public string CriteriaName { get; set; } = string.Empty;
        public decimal? ActualValue { get; set; }
        public bool IsPassed { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho việc xem danh sách đánh giá chất lượng
    /// </summary>
    public class QualityEvaluationListViewDto
    {
        public Guid EvaluationId { get; set; }
        public string EvaluationCode { get; set; } = string.Empty;
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string CoffeeTypeName { get; set; } = string.Empty;
        public string FarmerName { get; set; } = string.Empty;
        public string OverallResult { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public int PassedCriteria { get; set; }
        public int TotalCriteria { get; set; }
        public DateTime EvaluatedAt { get; set; }
        public string EvaluatorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
