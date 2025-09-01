using System;
using System.Collections.Generic;
using System.Linq;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs
{
    /// <summary>
    /// Kết quả đánh giá chất lượng tổng thể
    /// </summary>
    public class QualityEvaluationResult
    {
        public string EvaluationType { get; set; } = string.Empty;
        public DateTime? EvaluatedAt { get; set; }
        public List<QualityCriteriaResult> CriteriaResults { get; set; } = new();
        public string? ExpertNotes { get; set; }
        
        /// <summary>
        /// Điểm tổng hợp
        /// </summary>
        public decimal OverallScore => CalculateOverallScore(CriteriaResults);
        
        /// <summary>
        /// Kết quả tổng thể
        /// </summary>
        public string OverallResult => DetermineOverallResult(CriteriaResults);

        /// <summary>
        /// Tính điểm tổng hợp dựa trên severity của tiêu chí
        /// </summary>
        /// <param name="criteriaResults">Kết quả đánh giá từng tiêu chí</param>
        /// <returns>Điểm tổng hợp</returns>
        private static decimal CalculateOverallScore(List<QualityCriteriaResult> criteriaResults)
        {
            if (!criteriaResults.Any()) return 0;

            var totalWeight = criteriaResults.Sum(c => c.Severity == "Hard" ? 2.0m : 1.0m);
            var weightedScore = 0m;

            foreach (var criteria in criteriaResults)
            {
                var weight = criteria.Severity == "Hard" ? 2.0m : 1.0m;
                var score = criteria.IsPassed ? 100 : 0;
                weightedScore += score * weight;
            }

            return totalWeight > 0 ? weightedScore / totalWeight : 0;
        }

        /// <summary>
        /// Đánh giá kết quả tổng thể dựa trên tiêu chí
        /// </summary>
        /// <param name="criteriaResults">Kết quả đánh giá từng tiêu chí</param>
        /// <returns>Kết quả tổng thể: Pass, Fail, NeedsImprovement</returns>
        private static string DetermineOverallResult(List<QualityCriteriaResult> criteriaResults)
        {
            if (!criteriaResults.Any()) return "Unknown";

            var hardCriteria = criteriaResults.Where(c => c.Severity == "Hard").ToList();
            var softCriteria = criteriaResults.Where(c => c.Severity == "Soft").ToList();

            // Nếu có tiêu chí Hard không đạt → Fail
            if (hardCriteria.Any(c => !c.IsPassed))
                return "Fail";

            // Nếu tất cả Hard đạt và ít nhất 80% Soft đạt → Pass
            var softPassRate = softCriteria.Any() ? (decimal)softCriteria.Count(c => c.IsPassed) / softCriteria.Count : 1;
            if (softPassRate >= 0.8m)
                return "Pass";

            // Còn lại → NeedsImprovement
            return "NeedsImprovement";
        }
    }

    /// <summary>
    /// Kết quả đánh giá từng tiêu chí chất lượng
    /// </summary>
    public class QualityCriteriaResult
    {
        public string CriteriaName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Hard, Soft
        public string RuleGroup { get; set; } = string.Empty;
        public decimal? ActualValue { get; set; }
        public bool IsPassed { get; set; }
        public string? FailureReason { get; set; }
    }
}
