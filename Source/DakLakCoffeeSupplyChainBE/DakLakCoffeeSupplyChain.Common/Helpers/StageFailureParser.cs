using System;
using System.Linq;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public class StageFailureInfo
    {
        public int? FailedStageId { get; set; }
        public string FailedStageName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Recommendations { get; set; } = string.Empty;
        public bool IsFailure { get; set; }
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
                if (!int.TryParse(stageIdStr, out int stageId))
                    return null;

                return new StageFailureInfo
                {
                    FailedStageId = stageId,
                    FailedStageName = stageNamePart?.Replace(FAILED_STAGE_NAME_PREFIX, "") ?? "",
                    Details = detailsPart?.Replace(DETAILS_PREFIX, "") ?? "",
                    Recommendations = recommendationsPart?.Replace(RECOMMENDATIONS_PREFIX, "") ?? "",
                    IsFailure = true
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tạo format comments chuẩn cho failure
        /// </summary>
        /// <param name="stageId">ID của stage bị fail</param>
        /// <param name="stageName">Tên stage bị fail</param>
        /// <param name="details">Chi tiết vấn đề</param>
        /// <param name="recommendations">Khuyến nghị</param>
        /// <returns>Comments format chuẩn</returns>
        public static string CreateFailureComment(int stageId, string stageName, string details, string recommendations)
        {
            return $"{FAILED_STAGE_ID_PREFIX}{stageId}|{FAILED_STAGE_NAME_PREFIX}{stageName}|{DETAILS_PREFIX}{details}|{RECOMMENDATIONS_PREFIX}{recommendations}";
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
