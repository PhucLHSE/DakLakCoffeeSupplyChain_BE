namespace DakLakCoffeeSupplyChain.Common.Enum.CultivationRegistrationEnums
{
    /// <summary>
    /// Có 2 cột status ở cả bảng chính và phụ đều sẽ dùng chung enum này
    /// Nếu BM duyệt 1 detail và mà để nguyên không duyệt cái còn lại thì khi khi đã tạo hợp đồng, status của bảng chính sẽ được cập nhật
    /// </summary>
    public enum CultivationRegistrationStatus
    {
        Pending = 0,    // Trạng thái mặc định, chờ duyệt từ phía BM
        Approved = 1,     // Trạng thái sau khi đã được BM duyệt
        Cancelled = 2,     // Trạng thái tự farmer hủy bỏ trước khi BM duyệt
        Rejected = 3,   // Trạng thái sau khi bị BM từ chối
        Unknown = 4,    // Trạng thái khi hệ thống bị lỗi, không được lưu trong database
    }
}
