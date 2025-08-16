using System;
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
        
        // 🔧 DEPRECATED: Giữ lại để backward compatibility
        [Obsolete("Sử dụng FailedOrderIndex thay thế")]
        public int? FailedStageId_Old => FailedOrderIndex;
    }

    public static class StageFailureParser
    {
        private const string FAILED_STAGE_ID_PREFIX = "FAILED_STAGE_ID:";
        private const string FAILED_STAGE_NAME_PREFIX = "FAILED_STAGE_NAME:";
        private const string DETAILS_PREFIX = "DETAILS:";
        private const string RECOMMENDATIONS_PREFIX = "RECOMMENDATIONS:";

        /// <summary>
        /// Parse thông tin stage failure từ comments
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

                if (stageIdPart == null) return null;

                var stageIdStr = stageIdPart.Replace(FAILED_STAGE_ID_PREFIX, "");
                if (!int.TryParse(stageIdStr, out int orderIndex))
                    return null;

                return new StageFailureInfo
                {
                    FailedOrderIndex = orderIndex,
                    FailedStageId = null, // Sẽ được set từ service
                    FailedStageName = stageNamePart?.Replace(FAILED_STAGE_NAME_PREFIX, "") ?? "",
                    FailureDetails = detailsPart?.Replace(DETAILS_PREFIX, "") ?? "",
                    Recommendations = recommendationsPart?.Replace(RECOMMENDATIONS_PREFIX, "") ?? "",
                    IsFailure = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error parsing failure comment: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tạo format comments chuẩn cho failure
        /// </summary>
        /// <param name="orderIndex">OrderIndex của stage bị fail</param>
        /// <param name="stageName">Tên stage bị fail</param>
        /// <param name="details">Chi tiết vấn đề</param>
        /// <param name="recommendations">Khuyến nghị</param>
        /// <returns>Comments format chuẩn</returns>
        public static string CreateFailureComment(int orderIndex, string stageName, string details, string recommendations)
        {
            return $"{FAILED_STAGE_ID_PREFIX}{orderIndex}|{FAILED_STAGE_NAME_PREFIX}{stageName}|{DETAILS_PREFIX}{details}|{RECOMMENDATIONS_PREFIX}{recommendations}";
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
