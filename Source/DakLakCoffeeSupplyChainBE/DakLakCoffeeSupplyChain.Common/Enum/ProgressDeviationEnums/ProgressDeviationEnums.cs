namespace DakLakCoffeeSupplyChain.Common.Enum.ProgressDeviationEnums
{
    /// <summary>
    /// Trạng thái sai lệch tiến độ
    /// </summary>
    public enum DeviationStatus
    {
        OnTime = 0,        // Đúng tiến độ
        Ahead = 1,         // Vượt trước
        Behind = 2,        // Chậm tiến độ
        Critical = 3       // Chậm nghiêm trọng
    }

    /// <summary>
    /// Mức độ sai lệch
    /// </summary>
    public enum DeviationLevel
    {
        Low = 0,           // Thấp (0-10%)
        Medium = 1,        // Trung bình (10-25%)
        High = 2,          // Cao (25-50%)
        Critical = 3       // Nghiêm trọng (>50%)
    }

    /// <summary>
    /// Loại khuyến nghị
    /// </summary>
    public enum RecommendationCategory
    {
        Timing = 0,        // Về thời gian
        Yield = 1,         // Về sản lượng
        Quality = 2,       // Về chất lượng
        Process = 3,       // Về quy trình
        Technology = 4,    // Về công nghệ
        Resource = 5       // Về nguồn lực
    }

    /// <summary>
    /// Mức độ ưu tiên khuyến nghị
    /// </summary>
    public enum RecommendationPriority
    {
        Low = 0,           // Thấp
        Medium = 1,        // Trung bình
        High = 2,          // Cao
        Critical = 3       // Nghiêm trọng
    }

    /// <summary>
    /// Mức độ tác động
    /// </summary>
    public enum ImpactLevel
    {
        Low = 0,           // Thấp
        Medium = 1,        // Trung bình
        High = 2           // Cao
    }

    /// <summary>
    /// Mức độ nỗ lực thực hiện
    /// </summary>
    public enum EffortLevel
    {
        Low = 0,           // Thấp
        Medium = 1,        // Trung bình
        High = 2           // Cao
    }
}
