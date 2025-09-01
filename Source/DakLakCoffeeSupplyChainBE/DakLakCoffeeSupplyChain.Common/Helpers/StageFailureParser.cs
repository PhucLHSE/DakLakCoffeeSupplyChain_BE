using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchEvalutionDTOs;

namespace DakLakCoffeeSupplyChain.Common.Helpers
    {
        /// <summary>
    /// Helper để xử lý đánh giá chất lượng ProcessingBatch
        /// </summary>
    public static class QualityEvaluationHelper
    {
        // ========== CONSTANTS CHO FORMAT COMMENT MỚI ==========
        private const string EVALUATION_TYPE_PREFIX = "EVALUATION_TYPE:";
        private const string EVALUATED_AT_PREFIX = "EVALUATED_AT:";
        private const string CRITERIA_PREFIX = "CRITERIA:";
        private const string DISPLAY_NAME_PREFIX = "DISPLAY_NAME:";
        private const string MIN_PREFIX = "MIN:";
        private const string MAX_PREFIX = "MAX:";
        private const string UNIT_PREFIX = "UNIT:";
        private const string OPERATOR_PREFIX = "OPERATOR:";
        private const string SEVERITY_PREFIX = "SEVERITY:";
        private const string RULE_GROUP_PREFIX = "RULE_GROUP:";
        private const string ACTUAL_PREFIX = "ACTUAL:";
        private const string RESULT_PREFIX = "RESULT:";
        private const string REASON_PREFIX = "REASON:";
        private const string EXPERT_NOTES_PREFIX = "EXPERT_NOTES:";

        // ========== LOGIC ĐÁNH GIÁ TIÊU CHÍ ==========
        
        /// <summary>
        /// Đánh giá tiêu chí dựa theo SystemConfiguration
        /// </summary>
        /// <param name="criterion">Tiêu chí từ SystemConfiguration</param>
        /// <param name="actualValue">Giá trị thực tế</param>
        /// <returns>True nếu đạt tiêu chí</returns>
        public static bool EvaluateCriteria(ProcessingBatchCriteriaViewDto criterion, decimal actualValue)
        {
            switch (criterion.Operator)
            {
                case "<=": // Nhỏ hơn hoặc bằng
                    return actualValue <= criterion.MaxValue;
                    
                case ">=": // Lớn hơn hoặc bằng  
                    return actualValue >= criterion.MinValue;
                    
                case "=": // Bằng chính xác
                    return actualValue == criterion.MinValue && actualValue == criterion.MaxValue;
                    
                case "<": // Nhỏ hơn
                    return actualValue < criterion.MaxValue;
                    
                case ">": // Lớn hơn
                    return actualValue > criterion.MinValue;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Tự động đánh giá các tiêu chí dựa trên actual values
        /// </summary>
        /// <param name="criteriaEvaluations">Danh sách tiêu chí cần đánh giá</param>
        /// <returns>Danh sách tiêu chí đã được đánh giá</returns>
        public static List<QualityCriteriaResult> AutoEvaluateCriteria(List<QualityCriteriaEvaluationDto> criteriaEvaluations)
        {
            var results = new List<QualityCriteriaResult>();
            
            if (criteriaEvaluations == null || !criteriaEvaluations.Any())
            {
                return results;
            }
            
            foreach (var evaluation in criteriaEvaluations)
            {
                if (evaluation == null) continue;
                
                try
                {
                    if (evaluation.ActualValue.HasValue)
                    {
                        // Tự động đánh giá nếu có actual value
                        var isPassed = EvaluateCriteria(new ProcessingBatchCriteriaViewDto
                        {
                            Name = evaluation.CriteriaName ?? "",
                            MinValue = evaluation.MinValue,
                            MaxValue = evaluation.MaxValue,
                            Operator = evaluation.Operator ?? ""
                        }, evaluation.ActualValue.Value);
                        
                        evaluation.IsPassed = isPassed;
                        
                        // Tự động tạo failure reason nếu không đạt
                        if (!isPassed)
                        {
                            evaluation.FailureReason = GenerateFailureReason(evaluation);
                        }
                    }
                    
                    // Chuyển đổi sang QualityCriteriaResult
                    results.Add(new QualityCriteriaResult
                    {
                        CriteriaName = evaluation.CriteriaName ?? "",
                        DisplayName = evaluation.Description ?? "",
                        MinValue = evaluation.MinValue,
                        MaxValue = evaluation.MaxValue,
                        Unit = evaluation.Unit ?? "",
                        Operator = evaluation.Operator ?? "",
                        Severity = evaluation.Severity ?? "",
                        RuleGroup = evaluation.RuleGroup ?? "",
                        ActualValue = evaluation.ActualValue,
                        IsPassed = evaluation.IsPassed,
                        FailureReason = evaluation.FailureReason ?? ""
                    });
            }
            catch (Exception ex)
            {
                    // Log lỗi và tiếp tục với tiêu chí tiếp theo
                    Console.WriteLine($"Lỗi đánh giá tiêu chí {evaluation.CriteriaName}: {ex.Message}");
                    
                    // Thêm kết quả mặc định
                    results.Add(new QualityCriteriaResult
                    {
                        CriteriaName = evaluation.CriteriaName ?? "",
                        DisplayName = evaluation.Description ?? "",
                        MinValue = evaluation.MinValue,
                        MaxValue = evaluation.MaxValue,
                        Unit = evaluation.Unit ?? "",
                        Operator = evaluation.Operator ?? "",
                        Severity = evaluation.Severity ?? "",
                        RuleGroup = evaluation.RuleGroup ?? "",
                        ActualValue = evaluation.ActualValue,
                        IsPassed = false,
                        FailureReason = $"Lỗi đánh giá: {ex.Message}"
                    });
                }
            }
            
            return results;
        }

        /// <summary>
        /// Tự động tạo lý do không đạt cho tiêu chí
        /// </summary>
        /// <param name="evaluation">Tiêu chí cần tạo lý do</param>
        /// <returns>Lý do không đạt</returns>
        private static string GenerateFailureReason(QualityCriteriaEvaluationDto evaluation)
        {
            if (!evaluation.ActualValue.HasValue) return "Không có giá trị thực tế";
            
            var actual = evaluation.ActualValue.Value;
            
            switch (evaluation.Operator)
            {
                case "<=":
                    if (evaluation.MaxValue.HasValue && actual > evaluation.MaxValue.Value)
                        return $"{evaluation.Description}: {actual} {evaluation.Unit} > {evaluation.MaxValue.Value} {evaluation.Unit} (vượt quá giới hạn)";
                    break;
                    
                case ">=":
                    if (evaluation.MinValue.HasValue && actual < evaluation.MinValue.Value)
                        return $"{evaluation.Description}: {actual} {evaluation.Unit} < {evaluation.MinValue.Value} {evaluation.Unit} (thấp hơn giới hạn)";
                    break;
                    
                case "=":
                    if (evaluation.MinValue.HasValue && evaluation.MaxValue.HasValue)
                    {
                        if (actual != evaluation.MinValue.Value)
                            return $"{evaluation.Description}: {actual} {evaluation.Unit} ≠ {evaluation.MinValue.Value} {evaluation.Unit} (không đúng giá trị yêu cầu)";
                    }
                    break;
                    
                case "<":
                    if (evaluation.MaxValue.HasValue && actual >= evaluation.MaxValue.Value)
                        return $"{evaluation.Description}: {actual} {evaluation.Unit} >= {evaluation.MaxValue.Value} {evaluation.Unit} (không nhỏ hơn giới hạn)";
                    break;
                    
                case ">":
                    if (evaluation.MinValue.HasValue && actual <= evaluation.MinValue.Value)
                        return $"{evaluation.Description}: {actual} {evaluation.Unit} <= {evaluation.MinValue.Value} {evaluation.Unit} (không lớn hơn giới hạn)";
                    break;
            }
            
            return $"{evaluation.Description}: Không đạt tiêu chí";
        }

        /// <summary>
        /// Tính điểm tổng hợp dựa trên severity của tiêu chí
        /// </summary>
        /// <param name="criteriaResults">Kết quả đánh giá từng tiêu chí</param>
        /// <returns>Điểm tổng hợp</returns>
        public static decimal CalculateOverallScore(List<QualityCriteriaResult> criteriaResults)
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
        public static string DetermineOverallResult(List<QualityCriteriaResult> criteriaResults)
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

        // ========== FORMAT COMMENT MỚI ==========

        /// <summary>
        /// Tạo comment đánh giá chất lượng theo format mới
        /// </summary>
        /// <param name="criteriaResults">Kết quả đánh giá từng tiêu chí</param>
        /// <param name="expertNotes">Ghi chú của expert</param>
        /// <returns>Comment format mới</returns>
        public static string CreateQualityEvaluationComment(
            List<QualityCriteriaResult> criteriaResults,
            string? expertNotes = null)
        {
            try
            {
                var comment = new StringBuilder();
                
                // Header
                comment.Append($"{EVALUATION_TYPE_PREFIX}ProcessingBatch");
                comment.Append($"|{EVALUATED_AT_PREFIX}{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                // Chi tiết từng tiêu chí
                foreach (var criteria in criteriaResults)
                {
                    if (criteria == null) continue;
                    
                    comment.Append($"|{CRITERIA_PREFIX}{criteria.CriteriaName ?? ""}");
                    comment.Append($"|{DISPLAY_NAME_PREFIX}{criteria.DisplayName ?? ""}");
                    comment.Append($"|{MIN_PREFIX}{criteria.MinValue?.ToString() ?? "NULL"}");
                    comment.Append($"|{MAX_PREFIX}{criteria.MaxValue?.ToString() ?? "NULL"}");
                    comment.Append($"|{UNIT_PREFIX}{criteria.Unit ?? ""}");
                    comment.Append($"|{OPERATOR_PREFIX}{criteria.Operator ?? ""}");
                    comment.Append($"|{SEVERITY_PREFIX}{criteria.Severity ?? ""}");
                    comment.Append($"|{RULE_GROUP_PREFIX}{criteria.RuleGroup ?? ""}");
                    comment.Append($"|{ACTUAL_PREFIX}{criteria.ActualValue?.ToString() ?? "NULL"}");
                    comment.Append($"|{RESULT_PREFIX}{(criteria.IsPassed ? "PASS" : "FAIL")}");
                    comment.Append($"|{REASON_PREFIX}{criteria.FailureReason ?? ""}");
                }
                
                // Ghi chú của expert
                if (!string.IsNullOrEmpty(expertNotes))
                {
                    comment.Append($"|{EXPERT_NOTES_PREFIX}{expertNotes}");
                }
                
                return comment.ToString();
            }
            catch (Exception ex)
            {
                // Log lỗi và trả về comment đơn giản
                Console.WriteLine($"Lỗi tạo quality evaluation comment: {ex.Message}");
                return $"{EVALUATION_TYPE_PREFIX}ProcessingBatch|{EVALUATED_AT_PREFIX}{DateTime.Now:yyyy-MM-dd HH:mm:ss}|ERROR:{ex.Message}";
            }
        }

        // ========== PARSE COMMENT MỚI ==========

        /// <summary>
        /// Parse comment đánh giá chất lượng từ format mới
        /// </summary>
        /// <param name="comment">Comment cần parse</param>
        /// <returns>Kết quả đánh giá chất lượng</returns>
        public static QualityEvaluationResult? ParseQualityEvaluationComment(string? comment)
        {
            if (string.IsNullOrEmpty(comment) || !comment.Contains(EVALUATION_TYPE_PREFIX))
                return null;

            try
            {
                var parts = comment.Split('|');
                var result = new QualityEvaluationResult();
                
                foreach (var part in parts)
                {
                    if (part.StartsWith(EVALUATION_TYPE_PREFIX))
                    {
                        result.EvaluationType = part.Replace(EVALUATION_TYPE_PREFIX, "");
                    }
                    else if (part.StartsWith(EVALUATED_AT_PREFIX))
                    {
                        var dateStr = part.Replace(EVALUATED_AT_PREFIX, "");
                        if (DateTime.TryParse(dateStr, out DateTime evaluatedAt))
                        {
                            result.EvaluatedAt = evaluatedAt;
                        }
                    }
                    else if (part.StartsWith(CRITERIA_PREFIX))
                    {
                        // Bắt đầu tiêu chí mới
                        var currentCriteria = new QualityCriteriaResult();
                        currentCriteria.CriteriaName = part.Replace(CRITERIA_PREFIX, "");
                        
                        // Parse các thuộc tính của tiêu chí này
                        var criteriaIndex = Array.IndexOf(parts, part);
                        ParseCriteriaProperties(parts, criteriaIndex, currentCriteria);
                        
                        result.CriteriaResults.Add(currentCriteria);
                    }
                    else if (part.StartsWith(EXPERT_NOTES_PREFIX))
                    {
                        result.ExpertNotes = part.Replace(EXPERT_NOTES_PREFIX, "");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error parsing quality evaluation comment: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse thuộc tính của một tiêu chí
        /// </summary>
        private static void ParseCriteriaProperties(string[] parts, int startIndex, QualityCriteriaResult criteria)
        {
            for (int i = startIndex + 1; i < parts.Length; i++)
            {
                var part = parts[i];
                
                if (part.StartsWith(CRITERIA_PREFIX))
                {
                    // Gặp tiêu chí mới → dừng
                    break;
                }
                else if (part.StartsWith(EXPERT_NOTES_PREFIX))
                {
                    // Gặp expert notes → dừng
                    break;
                }
                else if (part.StartsWith(DISPLAY_NAME_PREFIX))
                {
                    criteria.DisplayName = part.Replace(DISPLAY_NAME_PREFIX, "");
                }
                else if (part.StartsWith(MIN_PREFIX))
                {
                    var minStr = part.Replace(MIN_PREFIX, "");
                    if (decimal.TryParse(minStr, out decimal minValue))
                    {
                        criteria.MinValue = minValue;
                    }
                }
                else if (part.StartsWith(MAX_PREFIX))
                {
                    var maxStr = part.Replace(MAX_PREFIX, "");
                    if (decimal.TryParse(maxStr, out decimal maxValue))
                    {
                        criteria.MaxValue = maxValue;
                    }
                }
                else if (part.StartsWith(UNIT_PREFIX))
                {
                    criteria.Unit = part.Replace(UNIT_PREFIX, "");
                }
                else if (part.StartsWith(OPERATOR_PREFIX))
                {
                    criteria.Operator = part.Replace(OPERATOR_PREFIX, "");
                }
                else if (part.StartsWith(SEVERITY_PREFIX))
                {
                    criteria.Severity = part.Replace(SEVERITY_PREFIX, "");
                }
                else if (part.StartsWith(RULE_GROUP_PREFIX))
                {
                    criteria.RuleGroup = part.Replace(RULE_GROUP_PREFIX, "");
                }
                else if (part.StartsWith(ACTUAL_PREFIX))
                {
                    var actualStr = part.Replace(ACTUAL_PREFIX, "");
                    if (decimal.TryParse(actualStr, out decimal actualValue))
                    {
                        criteria.ActualValue = actualValue;
                    }
                }
                else if (part.StartsWith(RESULT_PREFIX))
                {
                    var resultStr = part.Replace(RESULT_PREFIX, "");
                    criteria.IsPassed = resultStr == "PASS";
                }
                else if (part.StartsWith(REASON_PREFIX))
                {
                    criteria.FailureReason = part.Replace(REASON_PREFIX, "");
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem comment có chứa thông tin đánh giá chất lượng không
        /// </summary>
        /// <param name="comment">Comment cần kiểm tra</param>
        /// <returns>True nếu là quality evaluation comment</returns>
        public static bool IsQualityEvaluationComment(string? comment)
        {
            return !string.IsNullOrEmpty(comment) && comment.Contains(EVALUATION_TYPE_PREFIX);
        }

        /// <summary>
        /// Tạo comment đánh giá chất lượng từ ProcessingBatchCriteriaViewDto
        /// </summary>
        /// <param name="criteria">Danh sách tiêu chí từ SystemConfiguration</param>
        /// <param name="expertNotes">Ghi chú của expert</param>
        /// <returns>Comment format mới</returns>
        public static string CreateQualityEvaluationCommentFromCriteria(
            List<ProcessingBatchCriteriaViewDto> criteria,
            string? expertNotes = null)
        {
            // Chuyển đổi sang QualityCriteriaResult với actual values = null
            var criteriaResults = criteria.Select(c => new QualityCriteriaResult
            {
                CriteriaName = c.Name,
                DisplayName = c.Description,
                MinValue = c.MinValue,
                MaxValue = c.MaxValue,
                Unit = c.Unit,
                Operator = c.Operator,
                Severity = c.Severity,
                RuleGroup = c.RuleGroup,
                ActualValue = null,
                IsPassed = false,
                FailureReason = null
            }).ToList();

            return CreateQualityEvaluationComment(criteriaResults, expertNotes);
        }

        /// <summary>
        /// Tạo DTO cho frontend từ ProcessingBatchCriteriaViewDto
        /// </summary>
        /// <param name="criteria">Danh sách tiêu chí từ SystemConfiguration</param>
        /// <returns>Danh sách QualityCriteriaEvaluationDto cho frontend</returns>
        public static List<QualityCriteriaEvaluationDto> CreateEvaluationDtoForFrontend(
            List<ProcessingBatchCriteriaViewDto> criteria)
        {
            return criteria.Select(c => new QualityCriteriaEvaluationDto
            {
                CriteriaId = c.Id,
                CriteriaName = c.Name,
                Description = c.Description,
                MinValue = c.MinValue,
                MaxValue = c.MaxValue,
                Unit = c.Unit,
                Operator = c.Operator,
                Severity = c.Severity,
                RuleGroup = c.RuleGroup,
                ActualValue = null,
                IsPassed = false,
                FailureReason = null,
                Notes = null
            }).ToList();
        }

        /// <summary>
        /// Tạo DTO cho frontend từ comment đã lưu trong database
        /// </summary>
        /// <param name="comment">Comment từ database</param>
        /// <returns>Danh sách QualityCriteriaEvaluationDto cho frontend</returns>
        public static List<QualityCriteriaEvaluationDto> CreateEvaluationDtoFromComment(string? comment)
        {
            var evaluationResult = ParseQualityEvaluationComment(comment);
            if (evaluationResult == null) return new List<QualityCriteriaEvaluationDto>();

            return evaluationResult.CriteriaResults.Select(c => new QualityCriteriaEvaluationDto
            {
                CriteriaId = 0, // Không có ID từ comment
                CriteriaName = c.CriteriaName,
                Description = c.DisplayName,
                MinValue = c.MinValue,
                MaxValue = c.MaxValue,
                Unit = c.Unit,
                Operator = c.Operator,
                Severity = c.Severity,
                RuleGroup = c.RuleGroup,
                ActualValue = c.ActualValue,
                IsPassed = c.IsPassed,
                FailureReason = c.FailureReason,
                Notes = null
            }).ToList();
        }
    }
}
