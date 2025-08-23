using System;
using System.Collections.Generic;
using System.Linq;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public class StageFailureInfo
    {
        /// <summary>
        /// OrderIndex của stage bị fail (thứ tự trong method)
        /// </summary>
        public int FailedOrderIndex { get; set; }
        
        /// <summary>
        /// StageId thực tế của stage bị fail (từ database)
        /// </summary>
        public int? FailedStageId { get; set; }
        
        /// <summary>
        /// Tên stage bị fail
        /// </summary>
        public string FailedStageName { get; set; } = string.Empty;
        
        /// <summary>
        /// Chi tiết vấn đề
        /// </summary>
        public string FailureDetails { get; set; } = string.Empty;
        
        /// <summary>
        /// Khuyến nghị cải thiện
        /// </summary>
        public string Recommendations { get; set; } = string.Empty;
        
        /// <summary>
        /// Xác định đây có phải là failure comment không
        /// </summary>
        public bool IsFailure { get; set; }
        
        /// <summary>
        /// Danh sách tiêu chí bị fail
        /// </summary>
        public List<FailedCriteria> FailedCriteria { get; set; } = new();
        
        /// <summary>
        /// Lý do không đạt được chọn
        /// </summary>
        public List<string> SelectedFailureReasons { get; set; } = new();
        
        /// <summary>
        /// Điểm đánh giá tổng hợp
        /// </summary>
        public decimal OverallScore { get; set; }
        
        // 🔧 DEPRECATED: Giữ lại để backward compatibility
        [Obsolete("Sử dụng FailedOrderIndex thay thế")]
        public int? FailedStageId_Old => FailedOrderIndex;
    }
    
    public class FailedCriteria
    {
        /// <summary>
        /// ID tiêu chí
        /// </summary>
        public string CriteriaId { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên tiêu chí
        /// </summary>
        public string CriteriaName { get; set; } = string.Empty;
        
        /// <summary>
        /// Giá trị thực tế
        /// </summary>
        public decimal ActualValue { get; set; }
        
        /// <summary>
        /// Giá trị chuẩn
        /// </summary>
        public string ExpectedValue { get; set; } = string.Empty;
        
        /// <summary>
        /// Đơn vị
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        
        /// <summary>
        /// Lý do không đạt
        /// </summary>
        public string FailureReason { get; set; } = string.Empty;
    }

    public static class StageFailureParser
    {
        private const string FAILED_STAGE_ID_PREFIX = "FAILED_STAGE_ID:";
        private const string FAILED_STAGE_NAME_PREFIX = "FAILED_STAGE_NAME:";
        private const string DETAILS_PREFIX = "DETAILS:";
        private const string RECOMMENDATIONS_PREFIX = "RECOMMENDATIONS:";
        private const string FAILED_CRITERIA_PREFIX = "FAILED_CRITERIA:";
        private const string FAILURE_REASONS_PREFIX = "FAILURE_REASONS:";
        private const string OVERALL_SCORE_PREFIX = "OVERALL_SCORE:";

        /// <summary>
        /// Parse thông tin stage failure từ comments (phiên bản cải tiến)
        /// </summary>
        /// <param name="comments">Comments từ evaluation</param>
        /// <returns>StageFailureInfo hoặc null nếu không phải failure</returns>
        public static StageFailureInfo? ParseFailureFromComments(string? comments)
        {
            if (string.IsNullOrEmpty(comments) || !comments.Contains(FAILED_STAGE_ID_PREFIX))
                return null;

            try
            {
                var parts = comments.Split('|');
                
                var stageIdPart = parts.FirstOrDefault(p => p.StartsWith(FAILED_STAGE_ID_PREFIX));
                var stageNamePart = parts.FirstOrDefault(p => p.StartsWith(FAILED_STAGE_NAME_PREFIX));
                var detailsPart = parts.FirstOrDefault(p => p.StartsWith(DETAILS_PREFIX));
                var recommendationsPart = parts.FirstOrDefault(p => p.StartsWith(RECOMMENDATIONS_PREFIX));
                var failedCriteriaPart = parts.FirstOrDefault(p => p.StartsWith(FAILED_CRITERIA_PREFIX));
                var failureReasonsPart = parts.FirstOrDefault(p => p.StartsWith(FAILURE_REASONS_PREFIX));
                var overallScorePart = parts.FirstOrDefault(p => p.StartsWith(OVERALL_SCORE_PREFIX));

                if (stageIdPart == null) return null;

                var stageIdStr = stageIdPart.Replace(FAILED_STAGE_ID_PREFIX, "");
                if (!int.TryParse(stageIdStr, out int orderIndex))
                    return null;

                var failureInfo = new StageFailureInfo
                {
                    FailedOrderIndex = orderIndex,
                    FailedStageId = null, // Sẽ được set từ service
                    FailedStageName = stageNamePart?.Replace(FAILED_STAGE_NAME_PREFIX, "") ?? "",
                    FailureDetails = detailsPart?.Replace(DETAILS_PREFIX, "") ?? "",
                    Recommendations = recommendationsPart?.Replace(RECOMMENDATIONS_PREFIX, "") ?? "",
                    IsFailure = true
                };

                // Parse failed criteria
                if (!string.IsNullOrEmpty(failedCriteriaPart))
                {
                    failureInfo.FailedCriteria = ParseFailedCriteria(failedCriteriaPart.Replace(FAILED_CRITERIA_PREFIX, ""));
                }

                // Parse failure reasons
                if (!string.IsNullOrEmpty(failureReasonsPart))
                {
                    failureInfo.SelectedFailureReasons = ParseFailureReasons(failureReasonsPart.Replace(FAILURE_REASONS_PREFIX, ""));
                }

                // Parse overall score
                if (!string.IsNullOrEmpty(overallScorePart))
                {
                    var scoreStr = overallScorePart.Replace(OVERALL_SCORE_PREFIX, "");
                    if (decimal.TryParse(scoreStr, out decimal score))
                    {
                        failureInfo.OverallScore = score;
                    }
                }

                return failureInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error parsing failure comment: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tạo format comments chuẩn cho failure (phiên bản cải tiến)
        /// </summary>
        /// <param name="orderIndex">OrderIndex của stage bị fail</param>
        /// <param name="stageName">Tên stage bị fail</param>
        /// <param name="details">Chi tiết vấn đề</param>
        /// <param name="recommendations">Khuyến nghị</param>
        /// <param name="failedCriteria">Danh sách tiêu chí bị fail</param>
        /// <param name="failureReasons">Lý do không đạt</param>
        /// <param name="overallScore">Điểm tổng hợp</param>
        /// <returns>Comments format chuẩn</returns>
        public static string CreateFailureComment(
            int orderIndex, 
            string stageName, 
            string details, 
            string recommendations,
            List<FailedCriteria>? failedCriteria = null,
            List<string>? failureReasons = null,
            decimal overallScore = 0)
        {
            var comment = $"{FAILED_STAGE_ID_PREFIX}{orderIndex}|{FAILED_STAGE_NAME_PREFIX}{stageName}|{DETAILS_PREFIX}{details}|{RECOMMENDATIONS_PREFIX}{recommendations}";

            // Thêm failed criteria
            if (failedCriteria != null && failedCriteria.Any())
            {
                var criteriaStr = string.Join(";", failedCriteria.Select(c => $"{c.CriteriaId}:{c.CriteriaName}:{c.ActualValue}:{c.ExpectedValue}:{c.Unit}:{c.FailureReason}"));
                comment += $"|{FAILED_CRITERIA_PREFIX}{criteriaStr}";
            }

            // Thêm failure reasons
            if (failureReasons != null && failureReasons.Any())
            {
                var reasonsStr = string.Join(";", failureReasons);
                comment += $"|{FAILURE_REASONS_PREFIX}{reasonsStr}";
            }

            // Thêm overall score
            comment += $"|{OVERALL_SCORE_PREFIX}{overallScore}";

            return comment;
        }

        /// <summary>
        /// Tạo failure comment từ đánh giá tiêu chí (phiên bản cải tiến)
        /// </summary>
        /// <param name="orderIndex">OrderIndex của stage</param>
        /// <param name="stageName">Tên stage</param>
        /// <param name="criteriaResults">Kết quả đánh giá tiêu chí</param>
        /// <param name="selectedReasons">Lý do không đạt được chọn</param>
        /// <param name="selectedFailedCriteria">Tiêu chí bị fail được chọn cụ thể</param>
        /// <returns>Comments format chuẩn</returns>
        public static string CreateFailureCommentFromEvaluation(
            int orderIndex,
            string stageName,
            List<(dynamic criteria, string result, decimal actualValue)> criteriaResults,
            List<string> selectedReasons,
            List<string>? selectedFailedCriteria = null)
        {
            var failedCriteria = criteriaResults
                .Where(x => x.result == "Fail")
                .Select(x => new FailedCriteria
                {
                    CriteriaId = x.criteria.criteriaId,
                    CriteriaName = x.criteria.criteriaName,
                    ActualValue = x.actualValue,
                    ExpectedValue = $"{x.criteria.minValue}-{x.criteria.maxValue}",
                    Unit = x.criteria.unit,
                    FailureReason = GetFailureReasonForCriteria(x.criteria, x.actualValue)
                })
                .ToList();

            var overallScore = CalculateOverallScore(criteriaResults.Select(x => (x.criteria, x.result)).ToList());
            
            // Nếu có chọn tiêu chí cụ thể thì chỉ lấy những tiêu chí đó
            if (selectedFailedCriteria != null && selectedFailedCriteria.Any())
            {
                failedCriteria = failedCriteria.Where(c => selectedFailedCriteria.Contains(c.CriteriaName)).ToList();
            }
            
            var details = $"Đánh giá không đạt: {failedCriteria.Count}/{criteriaResults.Count} tiêu chí";
            var recommendations = "Cần cải thiện các tiêu chí không đạt để đảm bảo chất lượng";

            return CreateFailureComment(orderIndex, stageName, details, recommendations, failedCriteria, selectedReasons, overallScore);
        }

        /// <summary>
        /// Tính điểm tổng hợp từ kết quả đánh giá
        /// </summary>
        /// <param name="criteriaResults">Kết quả đánh giá tiêu chí</param>
        /// <returns>Điểm tổng hợp</returns>
        public static decimal CalculateOverallScore(List<(dynamic criteria, string result)> criteriaResults)
        {
            if (!criteriaResults.Any()) return 0;

            var totalScore = 0m;
            var totalWeight = 0m;

            foreach (var (criteria, result) in criteriaResults)
            {
                var weight = criteria.weight ?? 0m;
                totalWeight += weight;

                if (result == "Pass")
                {
                    totalScore += weight * 100;
                }
                else
                {
                    totalScore += weight * 0;
                }
            }

            return totalWeight > 0 ? totalScore / totalWeight : 0;
        }

        /// <summary>
        /// Lấy thông tin stage và tiêu chí để hiển thị khi fail
        /// </summary>
        /// <param name="stageCode">Mã stage</param>
        /// <returns>Thông tin stage và tiêu chí</returns>
        public static object GetStageFailureInfo(string stageCode)
        {
            var criteria = GetHardcodedCriteriaForStage(stageCode);
            var failureReasons = GetHardcodedFailureReasonsForStage(stageCode);
            
            return new
            {
                stageCode = stageCode,
                stageName = GetStageName(stageCode),
                criteria = criteria,
                failureReasons = failureReasons,
                totalCriteria = criteria.Count,
                totalFailureReasons = failureReasons.Count
            };
        }

        /// <summary>
        /// Lấy tên stage từ mã stage
        /// </summary>
        /// <param name="stageCode">Mã stage</param>
        /// <returns>Tên stage</returns>
        private static string GetStageName(string stageCode)
        {
            return stageCode.ToLower() switch
            {
                "harvest" => "Thu hoạch",
                "drying" => "Phơi khô",
                "hulling" => "Xay xát",
                "grading" => "Phân loại",
                "fermentation" => "Lên men",
                "washing" => "Rửa sạch",
                "pulping" => "Tách vỏ quả",
                "carbonic-ferment" => "Lên men carbonic",
                _ => stageCode
            };
        }

        /// <summary>
        /// Lấy tiêu chí đánh giá hardcoded cho stage
        /// </summary>
        /// <param name="stageCode">Mã stage</param>
        /// <returns>Danh sách tiêu chí</returns>
        private static List<object> GetHardcodedCriteriaForStage(string stageCode)
        {
            return stageCode.ToLower() switch
            {
                "harvest" => new List<object>
                {
                    new { criteriaId = "HARVEST_001", criteriaName = "Độ chín của quả", criteriaType = "Visual", minValue = 80, maxValue = 100, targetValue = 95, unit = "%", weight = 0.3, isRequired = true, description = "Tỷ lệ quả chín đỏ, không có quả xanh" },
                    new { criteriaId = "HARVEST_002", criteriaName = "Kích thước hạt", criteriaType = "Physical", minValue = 15, maxValue = 20, targetValue = 17, unit = "mm", weight = 0.2, isRequired = true, description = "Đường kính hạt cà phê" },
                    new { criteriaId = "HARVEST_003", criteriaName = "Tỷ lệ hạt lỗi", criteriaType = "Quality", minValue = 0, maxValue = 5, targetValue = 2, unit = "%", weight = 0.25, isRequired = true, description = "Hạt bị sâu, mốc, vỡ" },
                    new { criteriaId = "HARVEST_004", criteriaName = "Độ ẩm", criteriaType = "Chemical", minValue = 60, maxValue = 70, targetValue = 65, unit = "%", weight = 0.25, isRequired = true, description = "Độ ẩm của quả cà phê" }
                },
                "drying" => new List<object>
                {
                    new { criteriaId = "DRYING_001", criteriaName = "Độ ẩm cuối", criteriaType = "Chemical", minValue = 10, maxValue = 12, targetValue = 11, unit = "%", weight = 0.4, isRequired = true, description = "Độ ẩm sau khi phơi" },
                    new { criteriaId = "DRYING_002", criteriaName = "Nhiệt độ phơi", criteriaType = "Physical", minValue = 25, maxValue = 35, targetValue = 30, unit = "°C", weight = 0.3, isRequired = true, description = "Nhiệt độ môi trường phơi" },
                    new { criteriaId = "DRYING_003", criteriaName = "Thời gian phơi", criteriaType = "Process", minValue = 7, maxValue = 25, targetValue = 15, unit = "ngày", weight = 0.3, isRequired = true, description = "Số ngày phơi" }
                },
                "hulling" => new List<object>
                {
                    new { criteriaId = "HULLING_001", criteriaName = "Tỷ lệ hạt vỡ", criteriaType = "Quality", minValue = 0, maxValue = 3, targetValue = 1, unit = "%", weight = 0.4, isRequired = true, description = "Hạt bị vỡ trong quá trình xay" },
                    new { criteriaId = "HULLING_002", criteriaName = "Độ sạch vỏ", criteriaType = "Visual", minValue = 95, maxValue = 100, targetValue = 98, unit = "%", weight = 0.3, isRequired = true, description = "Tỷ lệ vỏ được tách sạch" },
                    new { criteriaId = "HULLING_003", criteriaName = "Kích thước hạt đồng đều", criteriaType = "Physical", minValue = 85, maxValue = 100, targetValue = 95, unit = "%", weight = 0.3, isRequired = true, description = "Tỷ lệ hạt có kích thước đồng đều" }
                },
                "grading" => new List<object>
                {
                    new { criteriaId = "GRADING_001", criteriaName = "Độ đồng đều kích thước", criteriaType = "Physical", minValue = 90, maxValue = 100, targetValue = 95, unit = "%", weight = 0.35, isRequired = true, description = "Tỷ lệ hạt cùng kích cỡ" },
                    new { criteriaId = "GRADING_002", criteriaName = "Màu sắc đồng đều", criteriaType = "Visual", minValue = 85, maxValue = 100, targetValue = 95, unit = "%", weight = 0.25, isRequired = true, description = "Tỷ lệ hạt cùng màu sắc" },
                    new { criteriaId = "GRADING_003", criteriaName = "Tỷ lệ hạt lỗi", criteriaType = "Quality", minValue = 0, maxValue = 2, targetValue = 0.5, unit = "%", weight = 0.4, isRequired = true, description = "Hạt bị đen, mốc, sâu" }
                },
                "fermentation" => new List<object>
                {
                    new { criteriaId = "FERMENT_001", criteriaName = "Thời gian lên men", criteriaType = "Process", minValue = 12, maxValue = 48, targetValue = 24, unit = "giờ", weight = 0.4, isRequired = true, description = "Thời gian lên men" },
                    new { criteriaId = "FERMENT_002", criteriaName = "Nhiệt độ lên men", criteriaType = "Physical", minValue = 18, maxValue = 25, targetValue = 22, unit = "°C", weight = 0.3, isRequired = true, description = "Nhiệt độ môi trường lên men" },
                    new { criteriaId = "FERMENT_003", criteriaName = "pH cuối", criteriaType = "Chemical", minValue = 4.5, maxValue = 5.5, targetValue = 5, unit = "", weight = 0.3, isRequired = true, description = "Độ pH sau lên men" }
                },
                "washing" => new List<object>
                {
                    new { criteriaId = "WASH_001", criteriaName = "Độ sạch bề mặt", criteriaType = "Visual", minValue = 95, maxValue = 100, targetValue = 98, unit = "%", weight = 0.5, isRequired = true, description = "Tỷ lệ hạt sạch bề mặt" },
                    new { criteriaId = "WASH_002", criteriaName = "Độ ẩm sau rửa", criteriaType = "Chemical", minValue = 50, maxValue = 60, targetValue = 55, unit = "%", weight = 0.5, isRequired = true, description = "Độ ẩm hạt sau rửa" }
                },
                "pulping" => new List<object>
                {
                    new { criteriaId = "PULPING_001", criteriaName = "Tỷ lệ tách vỏ thành công", criteriaType = "Quality", minValue = 95, maxValue = 100, targetValue = 98, unit = "%", weight = 0.4, isRequired = true, description = "Tỷ lệ quả được tách vỏ hoàn toàn" },
                    new { criteriaId = "PULPING_002", criteriaName = "Tỷ lệ hạt bị tổn thương", criteriaType = "Quality", minValue = 0, maxValue = 3, targetValue = 1, unit = "%", weight = 0.3, isRequired = true, description = "Hạt bị vỡ, nứt trong quá trình tách" },
                    new { criteriaId = "PULPING_003", criteriaName = "Năng suất tách vỏ", criteriaType = "Process", minValue = 200, maxValue = 800, targetValue = 500, unit = "kg/giờ", weight = 0.3, isRequired = true, description = "Khối lượng quả được xử lý mỗi giờ" }
                },
                "carbonic-ferment" => new List<object>
                {
                    new { criteriaId = "CARBONIC_FERMENT_001", criteriaName = "Thời gian lên men carbonic", criteriaType = "Process", minValue = 12, maxValue = 48, targetValue = 24, unit = "giờ", weight = 0.4, isRequired = true, description = "Thời gian lên men carbonic" },
                    new { criteriaId = "CARBONIC_FERMENT_002", criteriaName = "Nhiệt độ lên men carbonic", criteriaType = "Physical", minValue = 18, maxValue = 25, targetValue = 22, unit = "°C", weight = 0.3, isRequired = true, description = "Nhiệt độ môi trường lên men carbonic" },
                    new { criteriaId = "CARBONIC_FERMENT_003", criteriaName = "pH cuối", criteriaType = "Chemical", minValue = 4.5, maxValue = 5.5, targetValue = 5.0, unit = "", weight = 0.3, isRequired = true, description = "Độ pH sau lên men carbonic" }
                },
                _ => new List<object>()
            };
        }

        /// <summary>
        /// Lấy lý do không đạt hardcoded cho stage
        /// </summary>
        /// <param name="stageCode">Mã stage</param>
        /// <returns>Danh sách lý do</returns>
        private static List<object> GetHardcodedFailureReasonsForStage(string stageCode)
        {
            return stageCode.ToLower() switch
            {
                "harvest" => new List<object>
                {
                    new { reasonId = "HARVEST_FAIL_001", reasonCode = "UNRIPE_FRUITS", reasonName = "Quả chưa chín đủ", category = "Quality", severityLevel = 3, description = "Tỷ lệ quả xanh quá cao (>20%)" },
                    new { reasonId = "HARVEST_FAIL_002", reasonCode = "DAMAGED_FRUITS", reasonName = "Quả bị hư hỏng", category = "Quality", severityLevel = 4, description = "Quả bị sâu, mốc, vỡ" },
                    new { reasonId = "HARVEST_FAIL_003", reasonCode = "WRONG_HARVEST_TIME", reasonName = "Thời điểm thu hoạch không phù hợp", category = "Process", severityLevel = 2, description = "Thu hoạch quá sớm hoặc quá muộn" }
                },
                "drying" => new List<object>
                {
                    new { reasonId = "DRYING_FAIL_001", reasonCode = "HIGH_MOISTURE", reasonName = "Độ ẩm quá cao", category = "Quality", severityLevel = 4, description = "Độ ẩm >12%, cần phơi thêm" },
                    new { reasonId = "DRYING_FAIL_002", reasonCode = "OVER_DRYING", reasonName = "Phơi quá khô", category = "Quality", severityLevel = 3, description = "Độ ẩm <10%, hạt bị khô quá" },
                    new { reasonId = "DRYING_FAIL_003", reasonCode = "INSUFFICIENT_DRYING_TIME", reasonName = "Thời gian phơi không đủ", category = "Process", severityLevel = 3, description = "Phơi chưa đủ ngày" },
                    new { reasonId = "DRYING_FAIL_004", reasonCode = "POOR_DRYING_CONDITIONS", reasonName = "Điều kiện phơi không tốt", category = "Process", severityLevel = 2, description = "Thời tiết ẩm, thiếu nắng" }
                },
                "hulling" => new List<object>
                {
                    new { reasonId = "HULLING_FAIL_001", reasonCode = "HIGH_BREAKAGE_RATE", reasonName = "Tỷ lệ hạt vỡ cao", category = "Quality", severityLevel = 4, description = "Tỷ lệ hạt vỡ >3%" },
                    new { reasonId = "HULLING_FAIL_002", reasonCode = "INCOMPLETE_HULLING", reasonName = "Tách vỏ không hoàn toàn", category = "Quality", severityLevel = 3, description = "Còn sót vỏ trấu" },
                    new { reasonId = "HULLING_FAIL_003", reasonCode = "EQUIPMENT_ISSUE", reasonName = "Vấn đề thiết bị", category = "Equipment", severityLevel = 3, description = "Máy xay không hoạt động tốt" }
                },
                "grading" => new List<object>
                {
                    new { reasonId = "GRADING_FAIL_001", reasonCode = "INCONSISTENT_SIZE", reasonName = "Kích thước không đồng đều", category = "Quality", severityLevel = 3, description = "Hạt có kích thước khác nhau" },
                    new { reasonId = "GRADING_FAIL_002", reasonCode = "COLOR_VARIATION", reasonName = "Màu sắc không đồng đều", category = "Quality", severityLevel = 2, description = "Hạt có màu sắc khác nhau" },
                    new { reasonId = "GRADING_FAIL_003", reasonCode = "HIGH_DEFECT_RATE", reasonName = "Tỷ lệ hạt lỗi cao", category = "Quality", severityLevel = 4, description = "Nhiều hạt bị đen, mốc, sâu" }
                },
                "fermentation" => new List<object>
                {
                    new { reasonId = "FERMENT_FAIL_001", reasonCode = "OVER_FERMENTATION", reasonName = "Lên men quá lâu", category = "Process", severityLevel = 4, description = "Thời gian lên men >48 giờ" },
                    new { reasonId = "FERMENT_FAIL_002", reasonCode = "UNDER_FERMENTATION", reasonName = "Lên men chưa đủ", category = "Process", severityLevel = 3, description = "Thời gian lên men <12 giờ" },
                    new { reasonId = "FERMENT_FAIL_003", reasonCode = "WRONG_TEMPERATURE", reasonName = "Nhiệt độ không phù hợp", category = "Process", severityLevel = 3, description = "Nhiệt độ lên men không đúng" }
                },
                "washing" => new List<object>
                {
                    new { reasonId = "WASH_FAIL_001", reasonCode = "INCOMPLETE_CLEANING", reasonName = "Rửa chưa sạch", category = "Quality", severityLevel = 3, description = "Bề mặt hạt còn bẩn" },
                    new { reasonId = "WASH_FAIL_002", reasonCode = "EXCESSIVE_MOISTURE", reasonName = "Độ ẩm quá cao", category = "Quality", severityLevel = 2, description = "Độ ẩm sau rửa >60%" },
                    new { reasonId = "WASH_FAIL_003", reasonCode = "WATER_QUALITY_ISSUE", reasonName = "Chất lượng nước kém", category = "Process", severityLevel = 2, description = "Nước rửa không sạch" }
                },
                "pulping" => new List<object>
                {
                    new { reasonId = "PULPING_FAIL_001", reasonCode = "INCOMPLETE_PULPING", reasonName = "Tách vỏ không hoàn toàn", category = "Quality", severityLevel = 4, description = "Tỷ lệ tách vỏ <95%" },
                    new { reasonId = "PULPING_FAIL_002", reasonCode = "HIGH_DAMAGE_RATE", reasonName = "Tỷ lệ hạt tổn thương cao", category = "Quality", severityLevel = 4, description = "Tỷ lệ hạt vỡ, nứt >3%" },
                    new { reasonId = "PULPING_FAIL_003", reasonCode = "LOW_PROCESSING_RATE", reasonName = "Năng suất xử lý thấp", category = "Process", severityLevel = 3, description = "Năng suất <200 kg/giờ" },
                    new { reasonId = "PULPING_FAIL_004", reasonCode = "EQUIPMENT_MAINTENANCE", reasonName = "Thiết bị cần bảo trì", category = "Equipment", severityLevel = 3, description = "Máy tách vỏ hoạt động không tốt" }
                },
                "carbonic-ferment" => new List<object>
                {
                    new { reasonId = "CARBONIC_FERMENT_FAIL_001", reasonCode = "OVER_FERMENTATION", reasonName = "Lên men carbonic quá lâu", category = "Process", severityLevel = 4, description = "Thời gian lên men carbonic >48 giờ" },
                    new { reasonId = "CARBONIC_FERMENT_FAIL_002", reasonCode = "UNDER_FERMENTATION", reasonName = "Lên men carbonic chưa đủ", category = "Process", severityLevel = 3, description = "Thời gian lên men carbonic <12 giờ" },
                    new { reasonId = "CARBONIC_FERMENT_FAIL_003", reasonCode = "WRONG_TEMPERATURE", reasonName = "Nhiệt độ lên men carbonic không phù hợp", category = "Process", severityLevel = 3, description = "Nhiệt độ lên men carbonic không đúng" }
                },
                _ => new List<object>()
            };
        }

        /// <summary>
        /// Parse failed criteria từ string
        /// </summary>
        private static List<FailedCriteria> ParseFailedCriteria(string criteriaStr)
        {
            var result = new List<FailedCriteria>();
            
            if (string.IsNullOrEmpty(criteriaStr)) return result;

            var criteriaList = criteriaStr.Split(';');
            foreach (var criteria in criteriaList)
            {
                var parts = criteria.Split(':');
                if (parts.Length >= 6)
                {
                    result.Add(new FailedCriteria
                    {
                        CriteriaId = parts[0],
                        CriteriaName = parts[1],
                        ActualValue = decimal.TryParse(parts[2], out decimal actual) ? actual : 0,
                        ExpectedValue = parts[3],
                        Unit = parts[4],
                        FailureReason = parts[5]
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Parse failure reasons từ string
        /// </summary>
        private static List<string> ParseFailureReasons(string reasonsStr)
        {
            if (string.IsNullOrEmpty(reasonsStr)) return new List<string>();
            return reasonsStr.Split(';').ToList();
        }

        /// <summary>
        /// Lấy lý do không đạt cho tiêu chí cụ thể
        /// </summary>
        private static string GetFailureReasonForCriteria(dynamic criteria, decimal actualValue)
        {
            var minValue = criteria.minValue ?? 0m;
            var maxValue = criteria.maxValue ?? 0m;
            
            if (actualValue < minValue)
                return $"Giá trị thấp hơn chuẩn ({actualValue} < {minValue})";
            
            if (actualValue > maxValue)
                return $"Giá trị cao hơn chuẩn ({actualValue} > {maxValue})";
            
            return "Không đạt tiêu chí";
        }

        /// <summary>
        /// Kiểm tra xem comments có chứa thông tin failure không
        /// </summary>
        /// <param name="comments">Comments cần kiểm tra</param>
        /// <returns>True nếu là failure comment</returns>
        public static bool IsFailureComment(string? comments)
        {
            return !string.IsNullOrEmpty(comments) && comments.Contains(FAILED_STAGE_ID_PREFIX);
        }
    }
}
