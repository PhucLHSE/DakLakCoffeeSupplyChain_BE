using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using System.Text.Json;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public static class ProcessingHelper
    {
        /// <summary>
        /// Lấy tỷ lệ waste tối đa cho từng giai đoạn chế biến
        /// </summary>
        /// <param name="stageName">Tên giai đoạn</param>
        /// <returns>Tỷ lệ waste tối đa (%)</returns>
        public static double GetMaxWastePercentageForStage(string stageName)
        {
            return stageName?.ToLower() switch
            {
                "thu hoạch" => 20.0,
                "phơi" => 15.0,
                "xay vỏ" => 10.0,
                "phân loại" => 8.0,
                "đóng gói" => 5.0,
                "sàng lọc" => 12.0,
                "làm sạch" => 8.0,
                "rang" => 5.0,
                "nghiền" => 3.0,
                "đóng gói sản phẩm" => 2.0,
                _ => 15.0 // Default
            };
        }

        /// <summary>
        /// Xử lý parameters từ request (hỗ trợ cả single và multiple parameters)
        /// </summary>
        /// <param name="parameterName">Tên parameter đơn</param>
        /// <param name="parameterValue">Giá trị parameter đơn</param>
        /// <param name="unit">Đơn vị parameter đơn</param>
        /// <param name="recordedAt">Thời gian ghi nhận</param>
        /// <param name="parametersJson">JSON string chứa multiple parameters</param>
        /// <returns>Danh sách parameters đã xử lý</returns>
        public static List<ProcessingParameterInProgressDto> ProcessParameters(
            string? parameterName,
            string? parameterValue,
            string? unit,
            DateTime? recordedAt,
            string? parametersJson)
        {
            var parameters = new List<ProcessingParameterInProgressDto>();

            // Xử lý single parameter
            if (!string.IsNullOrEmpty(parameterName) && !string.IsNullOrEmpty(parameterValue))
            {
                parameters.Add(new ProcessingParameterInProgressDto
                {
                    ParameterName = parameterName,
                    ParameterValue = parameterValue,
                    Unit = unit ?? "kg",
                    RecordedAt = recordedAt ?? DateTime.UtcNow
                });
            }

            // Xử lý multiple parameters từ JSON
            if (!string.IsNullOrEmpty(parametersJson))
            {
                try
                {
                    var multipleParams = JsonSerializer.Deserialize<List<ProcessingParameterInProgressDto>>(parametersJson);
                    if (multipleParams != null && multipleParams.Any())
                    {
                        // Đảm bảo RecordedAt được set nếu chưa có
                        foreach (var param in multipleParams)
                        {
                            if (param.RecordedAt == default)
                                param.RecordedAt = recordedAt ?? DateTime.UtcNow;
                        }
                        
                        parameters.AddRange(multipleParams);
                    }
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException($"Lỗi parse JSON parameters: {ex.Message}", nameof(parametersJson), ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Lỗi xử lý parameters: {ex.Message}", ex);
                }
            }

            return parameters;
        }

        /// <summary>
        /// Validate parameters trước khi lưu
        /// </summary>
        /// <param name="parameters">Danh sách parameters cần validate</param>
        /// <returns>True nếu hợp lệ</returns>
        public static bool ValidateParameters(List<ProcessingParameterInProgressDto> parameters)
        {
            if (parameters == null || !parameters.Any())
                return true; // Không có parameters cũng OK

            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param.ParameterName))
                    return false;

                if (string.IsNullOrWhiteSpace(param.ParameterValue))
                    return false;

                if (string.IsNullOrWhiteSpace(param.Unit))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Lấy danh sách các giai đoạn chế biến phổ biến
        /// </summary>
        /// <returns>Dictionary chứa tên giai đoạn và tỷ lệ waste tối đa</returns>
        public static Dictionary<string, double> GetCommonProcessingStages()
        {
            return new Dictionary<string, double>
            {
                { "Thu hoạch", 20.0 },
                { "Phơi", 15.0 },
                { "Xay vỏ", 10.0 },
                { "Phân loại", 8.0 },
                { "Sàng lọc", 12.0 },
                { "Làm sạch", 8.0 },
                { "Rang", 5.0 },
                { "Nghiền", 3.0 },
                { "Đóng gói", 5.0 },
                { "Đóng gói sản phẩm", 2.0 }
            };
        }
    }
}
